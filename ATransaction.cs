using Aerospike.Client;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{

	[Flags]
	public enum TransactionStates
	{
		/// <summary>
		/// The Initial stat of the transaction
		/// </summary>
		NotInitized = 0x0000000,
		/// <summary>
		/// The transaction is ready for use or is actively being utilized. 
		/// </summary>
		Active = 0x0000001,
		/// <summary>
		/// The commit method was called and the commit is in progress.
		/// </summary>
		CommitRequested = 0x00000010,
		/// <summary>
		/// The commit is in progress but is in some retry state where some of the actions have been actually committed to the DB.
		/// </summary>
		PartiallyCommitted = CommitRequested | 0x0000100,
		/// <summary>
		/// The commit completed and all the actions were rolled forward.
		/// </summary>
		Committed = 0x0001000,
		/// <summary>
		/// The abort method was called and the abort is in progress. 
		/// </summary>
		AbortRequested = 0x0010000,
		/// <summary>
		/// The aborted was completed and the actions where rolled back.
		/// </summary>
		Aborted = 0x0100000,
		/// <summary>
		/// A Failure occurred during processing of a <see cref="ATransaction.Commit"/> or <see cref="ATransaction.Abort"/>
		/// </summary>
		Failed = 0x1000000
	}

	[Flags]
	public enum CommitResults
	{
		None = 0,
		Completed = 0x0001,
		/// <summary>
		/// The transaction was already completed
		/// </summary>
		AlreadyCommitted = Completed | 0x0010,
		/// <summary>
		/// The Server will roll forward this transaction
		/// </summary>
		RollForwardServer = 0x0100,
		/// <summary>
		/// The Server completed the roll forward of this transaction
		/// </summary>
		RollForwardServerCompleted = Completed | RollForwardServer,
		/// <summary>
		/// The commit completed via retries.
		/// </summary>
		CompletedRetry = 0x1000 | Completed,
		/// <summary>
		/// The Commit Failed. 
		/// </summary>
		Failed = 0x00010000,
		/// <summary>
		/// The Transaction was previously aborted. 
		/// </summary>
		PreviouslyAborted = 0x00100000 | Failed
	}

	[Flags]
	public enum AbortResults
	{
		None = 0,
		Completed = 0x0001,
		/// <summary>
		/// The transaction was already aborted
		/// </summary>
		AlreadyAborted = Completed | 0x0010,
		/// <summary>
		/// The Server will roll back this transaction
		/// </summary>
		RollBackServer = 0x0100,
		/// <summary>
		/// The Server completed the roll back of this transaction
		/// </summary>
		RollBackServerCompleted = Completed | RollBackServer,
		/// <summary>
		/// The Abort Failed. 
		/// </summary>
		Failed = 0x00010000,
		/// <summary>
		/// The Transaction was previously committed. 
		/// </summary>
		PreviouslyCommitted = 0x00100000 | Failed
	}

	/// <summary>
	/// Transaction State of a Record when it is part of an Aerospike Multiple Record Transaction (MRT)
	/// </summary>
	[Flags]
	public enum TransactionRecordStates
	{
		/// <summary>
		/// The record was NOT part of an Aerospike Multiple Record Transaction (MRT)
		/// </summary>
		NA = 0x0000,
		/// <summary>
		/// The record was read from an Aerospike Multiple Record Transaction (MRT)
		/// </summary>
		Read = 0x0001,
		/// <summary>
		/// The record was updated (delete, put, etc.) and is pending a commit or abort for an Aerospike Multiple Record Transaction (MRT)
		/// </summary>
		Update = 0x0010,
		/// <summary>
		/// The record is part of an Aerospike Multiple Record Transaction (MRT)
		/// Can be either a <see cref="Read"/> or <see cref="Update"/>
		/// </summary>
		MRT = Read | Update
	}

	/// <summary>
	/// Interface Used to create an Aerospike Multi-Record Transaction. 
	/// It wraps an <see cref="ANamespaceAccess"/> object which creates a set of new set of <see cref="Policy"/> that use <see cref="Txn"/> object.
	/// </summary>
	public interface IATransaction
	{
		/// <summary>
		/// Gets the aerospike <see cref="Aerospike.Client.Txn"/> instance or null to indicate that it is not within a transaction.
		/// </summary>
		/// <value>The aerospike <see cref="Aerospike.Client.Txn"/> instance or null</value>
		Txn Txn { get; }

		/// <summary>
		/// Gets or sets the commit retries on certain commit exceptions/errors. 
		/// It can be set up to the time of the <see cref="Commit"/>.
		/// 
		/// If this value is 0, no retries are allowed and an <see cref="RetryException"/> occurred.
		/// </summary>
		int CommitRetries { get; set; }
		/// <summary>
		/// Gets or sets the retry sleep in milliseconds.
		/// The sleep occurs between retries. 
		/// 
		/// If zero, no sleep occurs.
		/// </summary>
		/// <value>The retry sleep ms.</value>
		int RetrySleepMS { get; set; }

		/// <summary>
		/// MRT timeout in seconds. The timer starts when the MRT monitor record is created.
		/// This occurs when the first command in the MRT is executed. If the timeout is reached before
		/// a commit or abort is called, the server will expire and rollback the MRT.
		/// Defaults to 10 seconds.
		/// </summary>
		/// <value>The MRT timeout secs.</value>
		int MRTTimeoutSecs { get; }

		/// <summary>
		/// Returns the transaction identifier.
		/// </summary>
		long TransactionId { get; }

		/// <summary>
		/// The parent (Namespace instance, <see cref="ANamespaceAccess"/>) of this transaction.
		/// Could be a <see cref="ATransaction"/>.
		/// </summary>
		/// <value>The associated Namespace or Transaction instance</value>
		ANamespaceAccess Parent { get; }

		/// <summary>
		/// Returns the current state of the transaction.
		/// </summary>
		TransactionStates State { get; }

		/// <summary>
		/// Returns the status of the last <see cref="Commit"/>  action.
		/// </summary>
		CommitResults CommitStatus { get; }

		/// <summary>
		/// Returns the status of the last <see cref="Abort"/> action.
		/// </summary>
		AbortResults AbortStatus { get; }

		/// <summary>
		/// Attempt to commit the given multi-record transaction. First, the expected record versions are
		/// sent to the server nodes for verification.If all nodes return success, the command is
		/// committed. Otherwise, the transaction is retried based on <see cref="CommitRetries"/>.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="ATransaction.CreateTransaction(int, int, int)"/>
		/// <seealso cref="Abort()"/>
		/// <seealso cref="CommitRetries"/>
		/// <seealso cref="RetrySleepMS"/>
		/// <exception cref="AerospikeException"></exception>
		/// <exception cref="Exception"></exception>
		/// <exception cref="RetryException">Thrown when Commit <see cref="CommitRetries"/> is exceeded.</exception>
		CommitResults Commit();

		/// <summary>
		/// Abort and rollback the given multi-record transaction.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="ATransaction.CreateTransaction(int, int, int)"/>
		/// <seealso cref="Commit()"/>
		AbortResults Abort();

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="key">The record&apos;s key used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		(TransactionRecordStates state, long? generation) RecordState([NotNull] Key key);

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="key">The record&apos;s key used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		(TransactionRecordStates state, long?) RecordState([NotNull] APrimaryKey key);

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="record">The record used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		(TransactionRecordStates state, long?) RecordState([NotNull] ARecord record);

	}


	/// <summary>
	/// Used to create an Aerospike Multi-Record Transaction (MRT). 
	/// It wraps an <see cref="ANamespaceAccess"/> object which creates a set of new set of <see cref="Policy"/> that use <see cref="Txn"/> object.
	/// </summary>
	[DebuggerDisplay("{ToString()}")]
	public class ATransaction : ANamespaceAccess, IATransaction
	{
		
		#region Constructors

		internal ATransaction(ATransaction clone,
								Policy readPolicy = null,
								WritePolicy writePolicy = null,
								QueryPolicy queryPolicy = null,
								ScanPolicy scanPolicy = null)
			: base(clone,
					readPolicy is null
						? clone.DefaultReadPolicy
						: new(readPolicy)
							{
								Txn = clone.Txn
							},
					writePolicy is null
						? clone.DefaultWritePolicy
						: new(writePolicy)
						{
							Txn = clone.Txn
						},
					queryPolicy is null
						? clone.DefaultQueryPolicy
						: new(queryPolicy)
						{
							Txn = clone.Txn
						},
					scanPolicy is null
						? clone.DefaultScanPolicy
						: new(scanPolicy)
						{
							Txn = clone.Txn
						})
		{ 
			this.Txn = clone.Txn;
			this.State = clone.State;
			this.Parent = clone.Parent;
			this.CommitRetries = clone.CommitRetries;
			this.RetrySleepMS = clone.RetrySleepMS;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ATransaction"/> as an Aerospike transactional unit.
		/// If <see cref="Commit"/> method is not called the server will abort (rollback) this transaction.
		/// </summary>
		/// <param name="baseNS">Base Namespace instance</param>
		/// <param name="txn">The Aerospike <see cref="Txn"/> instance</param>
		/// <param name="commitretries"></param>
		/// <param name="retrySleepMS"></param>
		/// <exception cref="System.ArgumentNullException">txn</exception>
		/// <exception cref="System.ArgumentNullException">clone</exception>
		/// <seealso cref="CreateTransaction(int, int, int)"/>
		/// <seealso cref="Commit"/>
		/// <seealso cref="Abort"/>
		public ATransaction(ANamespaceAccess baseNS,
								Txn txn,
								int commitretries = 1,
								int retrySleepMS = 1000)
			: base(baseNS,
					new(baseNS.DefaultReadPolicy)
					{
						Txn = txn
					},
					new(baseNS.DefaultWritePolicy)
					{
						Txn = txn
					},
					new(baseNS.DefaultQueryPolicy)
					{
						Txn = txn
					},
					new(baseNS.DefaultScanPolicy)
					{
						Txn = txn
					})
		{
			this.Txn = txn;
			this.State = TransactionStates.Active;
			this.Parent = baseNS;
			this.CommitRetries = commitretries;
			this.RetrySleepMS = retrySleepMS;
		}

		/// <summary>
		/// Clones the specified instance providing new policies, if provided.
		/// This will used the same transaction id as the original.
		/// </summary>
		/// <param name="newReadPolicy">The new read policy.</param>
		/// <param name="newWritePolicy">The new write policy.</param>
		/// <param name="newQueryPolicy">The new query policy.</param>
		/// <param name="newScanPolicy">The new scan policy.</param>
		/// <returns>New clone of <see cref="ANamespaceAccess"/> instance.</returns>
		public override ATransaction Clone(Policy newReadPolicy = null,
											WritePolicy newWritePolicy = null,
											QueryPolicy newQueryPolicy = null,
											ScanPolicy newScanPolicy = null)
			=> new ATransaction(this,
								newReadPolicy,
								newWritePolicy,
								newQueryPolicy,
								newScanPolicy);
		#endregion

		/// <summary>
		/// Gets the aerospike <see cref="Aerospike.Client.Txn"/> instance
		/// </summary>
		/// <value>The aerospike <see cref="Aerospike.Client.Txn"/> instance</value>
		public Txn Txn { get; }

		/// <summary>
		/// Gets the aerospike <see cref="Aerospike.Client.Txn"/> instance or Null
		/// </summary>
		public override Txn GetAerospikeTxn() => this.Txn;

		/// <summary>
		/// Gets or sets the commit retries on certain commit exceptions/errors. 
		/// It can be set up to the time of the <see cref="Commit"/>.
		/// 
		/// If this value is 0, no retries are allowed and an <see cref="RetryException"/> occurred.
		/// </summary>
		public int CommitRetries { get; set; }
		/// <summary>
		/// Gets or sets the retry sleep in milliseconds.
		/// The sleep occurs between retries. 
		/// 
		/// If zero, no sleep occurs.
		/// </summary>
		/// <value>The retry sleep ms.</value>
		public int RetrySleepMS { get; set;  }

		/// <summary>
		/// MRT timeout in seconds. The timer starts when the MRT monitor record is created.
		/// This occurs when the first command in the MRT is executed. If the timeout is reached before
		/// a commit or abort is called, the server will expire and rollback the MRT.
		/// Defaults to 10 seconds.
		/// </summary>
		/// <value>The MRT timeout secs.</value>
		public int MRTTimeoutSecs => this.Txn.Timeout;

		/// <summary>
		/// Returns the transaction identifier.
		/// </summary>
		public long TransactionId => this.Txn.Id;

		/// <summary>
		/// The parent (Namespace instance, <see cref="ANamespaceAccess"/>) of this transaction.
		/// Could be a <see cref="ATransaction"/>.
		/// </summary>
		/// <value>The associated Namespace or Transaction instance</value>
		public ANamespaceAccess Parent {  get; }

		/// <summary>
		/// Returns the current state of the transaction.
		/// </summary>
		public TransactionStates State { get; private set; }

		/// <summary>
		/// Returns the status of the last <see cref="Commit"/>  action.
		/// </summary>
		public CommitResults CommitStatus { get; private set; }

		/// <summary>
		/// Returns the status of the last <see cref="Abort"/> action.
		/// </summary>
		public AbortResults AbortStatus { get; private set; }

		/// <summary>
		/// Creates an new Aerospike transaction.
		/// </summary>
		/// <param name="mrtTimeout">
		/// MRT timeout in seconds. The timer starts when the MRT monitor record is created.
		/// This occurs when the first command in the MRT is executed. If the timeout is reached before
		/// a commit or abort is called, the server will expire and rollback the MRT.
		/// Defaults to 10 seconds.
		/// </param>
		/// <param name="commitRetries">
		/// See <see cref="CommitRetries"/> property.
		/// </param>
		/// <param name="retrySleepMS">
		/// See <see cref="RetrySleepMS"/> property.
		/// </param>
		/// <returns>Transaction Namespace instance</returns>
		/// <seealso cref="Commit"/>
		/// <seealso cref="Abort"/>
		public override ATransaction CreateTransaction(int mrtTimeout = 10,
														int commitRetries = 1,
														int retrySleepMS = 1000) 
							=> new(this,
									new Txn() { Timeout = mrtTimeout },
									commitRetries,
									retrySleepMS);

		
		/// <summary>
		/// Attempt to commit the given multi-record transaction. First, the expected record versions are
		/// sent to the server nodes for verification.If all nodes return success, the command is
		/// committed. Otherwise, the transaction is retried based on <see cref="CommitRetries"/>.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="CreateTransaction(int, int, int)"/>
		/// <seealso cref="Abort()"/>
		/// <seealso cref="CommitRetries"/>
		/// <seealso cref="RetrySleepMS"/>
		/// <exception cref="AerospikeException"></exception>
		/// <exception cref="Exception"></exception>
		/// <exception cref="RetryException">Thrown when Commit <see cref="CommitRetries"/> is exceeded.</exception>
		public CommitResults Commit()
		{
			this.State = TransactionStates.CommitRequested;
			var result = CommitResults.None;
			
			CommitResults TryCommit(int attemp)
			{
				try
				{
					var aeResult = this.AerospikeConnection
										.AerospikeClient
										.Commit(this.Txn);

					switch(aeResult)
					{
						case Client.CommitStatus.CommitStatusType.OK:
							if(attemp == 1)
								result = CommitResults.Completed;
							else
								result = CommitResults.CompletedRetry;
							break;
						case Client.CommitStatus.CommitStatusType.ALREADY_COMMITTED:
							result = CommitResults.AlreadyCommitted;
							break;
						case Client.CommitStatus.CommitStatusType.ROLL_FORWARD_ABANDONED:
							result = CommitResults.RollForwardServer;
							break;
						case Client.CommitStatus.CommitStatusType.CLOSE_ABANDONED:
							result = CommitResults.RollForwardServerCompleted;
							break;
						default:
							throw new NotSupportedException($"Abort Result Code '{aeResult}' is Unknown");
					}
					this.State = TransactionStates.Committed;
				}
				catch(AerospikeException ae) when (ae.Result == ResultCode.TXN_ALREADY_ABORTED
													|| ae.Result == ResultCode.MRT_ABORTED)
				{
					this.State = TransactionStates.Aborted;
					return this.CommitStatus = CommitResults.PreviouslyAborted;
				}
				catch(AerospikeException ae) when(ae.Result == ResultCode.TXN_ALREADY_COMMITTED
													|| ae.Result == ResultCode.MRT_COMMITTED)
				{
					this.State = TransactionStates.Committed;
					return this.CommitStatus = CommitResults.AlreadyCommitted;
				}
				catch(AerospikeException ae) when (ae.InDoubt && this.CommitRetries > 0)
				{
					if(attemp < this.CommitRetries)
					{
						this.State = TransactionStates.PartiallyCommitted;
						if(this.RetrySleepMS > 0)
							Thread.Sleep(this.RetrySleepMS);
						else
							Thread.Sleep(0);
						return TryCommit(attemp + 1);
					}

					this.State = TransactionStates.PartiallyCommitted;
					this.CommitStatus = CommitResults.Failed;
					throw new RetryException($"Attempted to Retry Commit for Txn Id '{this.TransactionId}' {attemp} times.",
												attemp,
												this,
												ae);
				}
				catch(Exception)
				{
					this.State = TransactionStates.Failed;
					this.CommitStatus = CommitResults.Failed;
					throw;
				}

				this.CommitStatus = result;
				return result;
			}
			
			return TryCommit(1);
		}

		/// <summary>
		/// Abort and rollback the given multi-record transaction.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="CreateTransaction(int, int, int)"/>
		/// <seealso cref="Commit()"/>
		public AbortResults Abort()
		{
			this.State = TransactionStates.AbortRequested;
			AbortResults result;

			try
			{
				var aeResult = this.AerospikeConnection
									.AerospikeClient
									.Abort(this.Txn);

				result = aeResult switch
				{
					Client.AbortStatus.AbortStatusType.OK => AbortResults.Completed,
					Client.AbortStatus.AbortStatusType.ALREADY_ABORTED => AbortResults.AlreadyAborted,
					Client.AbortStatus.AbortStatusType.ROLL_BACK_ABANDONED => AbortResults.RollBackServer,
					Client.AbortStatus.AbortStatusType.CLOSE_ABANDONED => AbortResults.RollBackServerCompleted,
					_ => throw new NotSupportedException($"Abort Result Code '{aeResult}' is Unknown"),
				};
				this.State = TransactionStates.Aborted;
			}
			catch(AerospikeException ae) when (ae.Result == ResultCode.TXN_ALREADY_ABORTED
												|| ae.Result == ResultCode.MRT_ABORTED)
			{
				this.State = TransactionStates.Aborted;
				return this.AbortStatus = AbortResults.AlreadyAborted;
			}
			catch(AerospikeException ae) when(ae.Result == ResultCode.TXN_ALREADY_COMMITTED
												|| ae.Result == ResultCode.MRT_COMMITTED)
			{
				this.State = TransactionStates.Committed;
				return this.AbortStatus = AbortResults.PreviouslyCommitted;
			}
			catch(Exception)
			{
				this.AbortStatus = AbortResults.Failed;
				this.State = TransactionStates.Failed;
				throw;
			}

			this.AbortStatus = result;
			return result;
		}

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="key">The record&apos;s key used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		public (TransactionRecordStates state, long? generation) RecordState([NotNull] Key key)
		{
			if(this.Txn.Writes.Contains(key))
				return (TransactionRecordStates.Update, null);
			else if(this.Txn.Reads.ContainsKey(key))
				return (TransactionRecordStates.Read, this.Txn.Reads[key]);

			return (TransactionRecordStates.NA, null);
		}

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="key">The record&apos;s key used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		public (TransactionRecordStates state, long?) RecordState([NotNull] APrimaryKey key)
			=> this.RecordState(key.AerospikeKey);

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="record">The record used to see if it is part of this transaction</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		public (TransactionRecordStates state, long?) RecordState([NotNull] ARecord record)
			=> this.RecordState(record.Aerospike.Key);

		/// <summary>
		/// Returns the <see cref="TransactionRecordStates"/> of a record's key.
		/// This is only valid when the <see cref="State"/> is <see cref="TransactionStates.Active"/>.
		/// </summary>
		/// <param name="setName">Name of the set or null to indicate the Aerospike Null set.</param>
		/// <param name="keyValue">The key&apos;s value.</param>
		/// <returns>
		/// Returns a Value Tuple:
		/// state (1st value)		-- The <see cref="TransactionRecordStates"/>
		/// generation (2nd value)	-- The record&apos;s generation, if it was read. Otherwise the value will be null.
		/// </returns>
		public (TransactionRecordStates state, long?) RecordState(string setName, [NotNull] dynamic keyValue)
			=> this.RecordState(Helpers.DetermineAerospikeKey(keyValue, this.Namespace, setName));

		public override string ToString()
		{
			if(this.BinNames.Length == 0)
				return $"TXN {this.Namespace}";

			return $"TXN {this.Namespace}{{{string.Join(',', this.BinNames)}}}";
		}

		public override object ToDump()
			=> LPU.ToExpando(this, include: "Namespace, " +
											"DBPlatform, " +
											"Txn, CommitRetries, RetrySleepMS, MRTTimeoutSecs, " +
											"State, CommitStatus, AbortStatus, " +
											"SetNames, " +
											"BinNames, " +
											"AerospikeConnection, " +
											"DefaultReadPolicy, " +
											"DefaultQueryPolicy, " +
											"DefaultScanPolicy, " +
											"DefaultWritePolicy");
	}

	public class RetryException : Exception
	{
		private RetryException()
		{
		}

		public RetryException(string message,
								int attemps,
								ATransaction trans,
								Exception innerException)
					: base(message, innerException)
		{
			this.Attemps = attemps;
			this.Trans = trans;
		}

		public int Attemps { get; }
		public ATransaction Trans { get; }

	}
}
