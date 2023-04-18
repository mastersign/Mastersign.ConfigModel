using System;
using System.Collections.Generic;

namespace Mastersign.ConfigModel
{
    internal static class StringComparerLookup
    {
        private static readonly Dictionary<StringComparison, StringComparer> _comparsionToComparer =
            new Dictionary<StringComparison, StringComparer>
            {
                        { StringComparison.CurrentCulture, StringComparer.CurrentCulture },
                        { StringComparison.CurrentCultureIgnoreCase, StringComparer.CurrentCultureIgnoreCase },
                        { StringComparison.InvariantCulture, StringComparer.InvariantCulture },
                        { StringComparison.InvariantCultureIgnoreCase, StringComparer.InvariantCultureIgnoreCase },
                        { StringComparison.Ordinal, StringComparer.Ordinal },
                        { StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase }
            };

        public static StringComparer From(StringComparison comparison) => _comparsionToComparer[comparison];
    }
}
