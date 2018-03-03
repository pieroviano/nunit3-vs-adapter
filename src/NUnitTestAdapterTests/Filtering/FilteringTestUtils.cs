﻿// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering
{
    public static class FilteringTestUtils
    {
        public static ITestCaseFilterExpression CreateVSTestFilterExpression(string filter)
        {
            var filterExpressionWrapperType = Type.GetType("Microsoft.VisualStudio.TestPlatform.Common.Filtering.FilterExpressionWrapper, Microsoft.VisualStudio.TestPlatform.Common", throwOnError: true);

            var filterExpressionWrapper =
                filterExpressionWrapperType.GetTypeInfo()
                .GetConstructor(new[] { typeof(string) })
                .Invoke(new object[] { filter });

            return (ITestCaseFilterExpression)
                Type.GetType("Microsoft.VisualStudio.TestPlatform.Common.Filtering.TestCaseFilterExpression, Microsoft.VisualStudio.TestPlatform.Common", throwOnError: true).GetTypeInfo()
                .GetConstructor(new[] { filterExpressionWrapperType })
                .Invoke(new object[] { filterExpressionWrapper });
        }

        public static TfsTestFilter CreateTestFilter(ITestCaseFilterExpression filterExpression)
        {
            var context = Substitute.For<IRunContext>();
            context.GetTestCaseFilter(null, null).ReturnsForAnyArgs(filterExpression);
            return new TfsTestFilter(context);
        }

        public static void AssertExpectedResult(ITestCaseFilterExpression filterExpression, IReadOnlyCollection<TestCase> testCases, IReadOnlyCollection<string> expectedMatchingTestNames)
        {
            var matchingTestCases = CreateTestFilter(filterExpression).CheckFilter(testCases);

            Assert.That(matchingTestCases.Select(t => t.FullyQualifiedName), Is.EquivalentTo(expectedMatchingTestNames));
        }

        public static IReadOnlyCollection<TestCase> ConvertTestCases(string xml)
        {
            using (var testConverter = new TestConverter(
                new TestLogger(new MessageLoggerStub()),
                FakeTestData.AssemblyPath,
                collectSourceInformation: false))
            {
                return testConverter.ConvertTestCases(xml);
            }
        }
    }
}
