using System;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassCanBeSealed.Global

namespace Types
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]

    public sealed class Test
    {
        public Test( int intPropertyValue)
        {
            IntProperty = intPropertyValue;
        }
        public Test(string stringPropertyValue)
        {
            StringProperty = stringPropertyValue;
            PrivateStringProperty = stringPropertyValue;
        }

        public string StringProperty { get; set; }
        private string PrivateStringProperty { get; set; }

        public int IntProperty { get; set; }
    }
}
