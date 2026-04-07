namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    [TestFixture]
    public sealed class LiteGraphTouchstoneFactTests : TouchstoneNunitBase
    {
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return LiteGraphTouchstoneSuites.All; }
        }

        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
