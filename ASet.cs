using System;
using System.Collections.Generic;
using System.Linq;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class ASet
    {

        public const string NullSetName = "NullSet";

        public ASet(string name)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "set");
        }

        public ASet()
        {
            this.Name = NullSetName;
            this.SafeName = Helpers.CheckName(this.Name, "Set");

            this.IsNullSet = true;
        }

        /// <summary>
        /// The DB Name of the Set
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The safe name of the set that can be used by a C# class name or property.
        /// </summary>
        public string SafeName { get; }

        /// <summary>
        /// Returns true t indicate a Null Set.
        /// </summary>
        public bool IsNullSet { get; }

        /// <summary>
        /// Returns the Secondary Indexes associated with this set.
        /// </summary>
        public IEnumerable<ASecondaryIndex> SIndexes { get; internal set; } = Enumerable.Empty<ASecondaryIndex>();

        public override string ToString()
        {
            return this.Name;
        }
    }
}
