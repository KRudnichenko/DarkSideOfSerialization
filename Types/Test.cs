namespace Types
{
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
