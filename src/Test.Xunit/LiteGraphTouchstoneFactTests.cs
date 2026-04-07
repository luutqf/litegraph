namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using Xunit;

    public sealed class LiteGraphTouchstoneFactTests : TouchstoneFactBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return LiteGraphTouchstoneSuites.All; }
        }

        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
