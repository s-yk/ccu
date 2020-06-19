namespace CcuCore
{
    public class AnalyzerFactory
    {
        public static IAnalyzer Create(bool hierarchy, bool verbose)
        {
            if (hierarchy)
            {
                return new StandardAnalyzer(verbose);
            }
            return new TypeOnlyAnalyzer(verbose);
        }
    }
}
