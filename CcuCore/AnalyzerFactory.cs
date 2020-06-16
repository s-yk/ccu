namespace CcuCore
{
    public class AnalyzerFactory
    {
        public static IAnalyzer Create(bool hierarchy)
        {
            if (hierarchy)
            {
                return new StandardAnalyzer();
            }
            return new TypeOnlyAnalyzer();
        }
    }
}
