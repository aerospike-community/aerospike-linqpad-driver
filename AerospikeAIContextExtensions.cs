using LINQPad;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	public static class AerospikeAIContextExtensions
	{
		public static AerospikeAIContext ToAIContext(this AClusterAccess cluster)
		{
			return AerospikeAIContext.From(cluster);
		}

		public static string ToAIContextMarkdown(
			this AClusterAccess cluster,
			AerospikeAIContextOptions options = null)
		{
			return AerospikeAIContext.From(cluster).ToMarkdown(options);
		}

		public static string BuildAIPrompt(
			this AClusterAccess cluster,
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null)
		{
			return AerospikeAIContext.From(cluster)
				.BuildPrompt(userRequest, options, systemInstruction);
		}

		private static System.Reflection.MethodInfo _linqAIAsk = null;
		private static System.Reflection.MethodInfo LinqPadPAIAsk
		{
			get
			{
				if(_linqAIAsk is null)
				{
					try
					{
						var aiClass = typeof(LINQPad.Util)
										.Assembly
										.GetType("LINQPad.Util+AI");
						var aiModel = typeof(LINQPad.Util)
										.Assembly
										.GetType("LINQPad.ObjectModel.AI.AIModel");
						_linqAIAsk = aiClass
										 .GetMethod("Ask",
													new[] {
												typeof(string),
												typeof(double),
												aiModel,
												typeof(bool)
													});
					}
					catch(Exception e)
					{
						LINQPad.Extensions.Dump(e, "Error accessing LINQPad's AI 'Ask' Method");
						_linqAIAsk = null;
					}
				}
				return _linqAIAsk;
			}
		}

		private static System.Reflection.MethodInfo _linqAIGetResponse = null;
		private static System.Reflection.MethodInfo LinqPadAIGetResponse
		{
			get
			{
				if(_linqAIGetResponse is null)
				{
					try
					{
						_linqAIGetResponse = typeof(LINQPad.Util)
													.Assembly
													.GetType("LINQPad.ObjectModel.AI.AIRequest")
													.GetMethod("GetResponseAsync",
																new[] {
															typeof(System.Threading.CancellationToken),
															typeof(System.Action<string>),
															typeof(System.Action<string>) });
					}
					catch(Exception e)
					{
						LINQPad.Extensions.Dump(e, "Error accessing LINQPad's AI 'GetResponse' Method");
						_linqAIGetResponse = null;
					}
				}
				return _linqAIGetResponse;
			}
		}

		private static System.Reflection.PropertyInfo _linqAIResponse = null;
		private static System.Reflection.PropertyInfo LinqPadPAIResponse
		{
			get
			{
				if(_linqAIResponse is null)
				{
					try
					{
						_linqAIResponse = typeof(LINQPad.Util)
													.Assembly
													.GetType("LINQPad.ObjectModel.AI.AIResponse")
													.GetProperty("Text");
					}
					catch(Exception e)
					{
						LINQPad.Extensions.Dump(e, "Error accessing LINQPad's AI 'Response.Text' Property");
						_linqAIResponse = null;
					}
				}
				return _linqAIResponse;
			}
		}

		/// <summary>
		/// Submits a prompt to LINQPad's internal AI API and returns the response as an <see cref="object"/>.
		/// </summary>
		/// <param name="request">
		/// The prompt text to send to the AI service.
		/// </param>
		/// <param name="progression">
		/// <see langword="true"/> to show progress updates in a LINQPad <c>DumpContainer</c>; otherwise, <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel request submission or response processing.
		/// </param>
		/// <returns>
		/// A task that completes with the LINQPad AI response instance (boxed as <see cref="object"/>).
		/// The returned task is canceled if <paramref name="cancellationToken"/> is canceled, or faulted if the LINQPad AI APIs are unavailable.
		/// </returns>
		public static Task<object> Submit(string request,
											bool progression = true,
											CancellationToken cancellationToken = default)
		{
			var spinner = new[] { "✶", "✸", "✹", "✺", "✹", "✷" };
			DumpContainer progress = progression ? new DumpContainer() : null;
			int counter = 0;

			if(cancellationToken.IsCancellationRequested)
			{
				if(progress != null)
				{
					progress.Content = "AI Request Canceled.";
					progress.Dump("AI Progress");
				}

				return Task.FromCanceled<object>(cancellationToken);
			}

			var aiRequest = LinqPadPAIAsk?.Invoke(null, new object[] { request, 0.0, null, false });

			if(aiRequest == null)
			{
				return Task.FromException<object>(
					new Exception("Unable to create AI Request. LINQPad's AI features may not be available or compatible with this version of LINQPad."));
			}

			if(progress != null)
			{
				progress.Content = "Starting...";
				progress.Dump("AI Progress");
			}

			void UpdateProgress(bool sending)
			{
				if(progress == null)
					return;

				if(counter % 5 == 0)
				{
					var frame = spinner[(counter / 5) % spinner.Length];
					var status = sending ? "Sending" : "Thinking";
					progress.Content = frame + " " + status + "...";
				}

				counter++;
			}

			var aiResponseTask = LinqPadAIGetResponse == null
									? null
									: LinqPadAIGetResponse.Invoke(
										aiRequest,
										new object[]
										{
											cancellationToken,
											progress == null ? null : new Action<string>(message => UpdateProgress(true)),
											progress == null ? null : new Action<string>(message => UpdateProgress(false))
										}) as Task;

			if(aiResponseTask == null)
			{
				return Task.FromException<object>(
						new Exception("Unable to get AI Response. LINQPad's AI features may not be available or compatible with this version of LINQPad."));
			}

			if(progress != null)
			{
				// Attach progress reporting, but do NOT replace aiResponseTask.
				aiResponseTask.ContinueWith(
					t =>
					{
						if(t.IsFaulted)
						{
							progress.Content = "Error: " + t.Exception?.GetBaseException().Message;
						}
						else if(t.IsCanceled)
						{
							progress.Content = "AI Request Canceled.";
						}
						else
						{
							progress.Content = "✓ AI Response Received.";
						}
					},
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}

			return ToObjectTask(aiResponseTask);
		}

		private static Task<object> ToObjectTask(Task task)
		{
			ArgumentNullException.ThrowIfNull(task);

			Type resultType = GetTaskResultType(task.GetType());

			if(resultType == null)
			{
				return task.ContinueWith(
					t =>
					{
						t.GetAwaiter().GetResult();
						return (object) null;
					},
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}

			var method = typeof(AerospikeAIContextExtensions)
				.GetMethod(
					nameof(ToObjectTaskGeneric),
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

			if(method == null)
			{
				return Task.FromException<object>(
					new MissingMethodException(nameof(AerospikeAIContextExtensions), nameof(ToObjectTaskGeneric)));
			}

			var genericMethod = method.MakeGenericMethod(resultType);

			return (Task<object>) genericMethod.Invoke(null, new object[] { task });
		}

		private static Task<object> ToObjectTaskGeneric<TResult>(Task<TResult> task)
		{
			return task.ContinueWith(
				t => (object) t.GetAwaiter().GetResult(),
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}

		private static Type GetTaskResultType(Type taskType)
		{
			while(taskType != null)
			{
				if(taskType.IsGenericType &&
					taskType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					return taskType.GetGenericArguments()[0];
				}

				taskType = taskType.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Submits a natural-language request to the configured AI context and returns the LINQPad response text.
		/// </summary>
		/// <param name="request">The user prompt to submit.</param>
		/// <param name="progression">
		/// <see langword="true"/> to enable progressive output while processing; otherwise, <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
		/// <returns>
		/// A task that resolves to the response text, or <see langword="null"/> when no LINQPad response accessor is available.
		/// </returns>
		public async static Task<string> Ask(string request,
												bool progression = true,
												CancellationToken cancellationToken = default)
		{
			var aiResult = await Submit(request, progression, cancellationToken);

			return LinqPadPAIResponse == null
					? null
					: LinqPadPAIResponse.GetValue(aiResult, null) as string;
		}
	}
}