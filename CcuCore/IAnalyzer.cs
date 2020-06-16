using System.Threading.Tasks;

namespace CcuCore
{
    public interface IAnalyzer
    {
        Task Analyze(string solutionPath);
    }
}
