﻿// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Core;
using NUnit.Util;

namespace NUnit.VisualStudio.TestAdapter
{
    [ExtensionUri(NUnitTestExecutor.ExecutorUri)]
    public sealed class NUnitTestExecutor : NUnitTestAdapter, ITestExecutor
    {
        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnitTestExecutor";

        /// <summary>
        /// The current NUnit TestRunner instance
        /// </summary>
        private TestRunner runner;

        #region ITestExecutor Members

        /// <summary>
        /// Runs the tests.
        /// </summary>
        /// <param name="sources">Sources to be run.</param>
        /// <param name="runContext">Context to use when executing the tests.</param>
        /// <param param name="testLog">Test log to send results and messages through</param>
        //void ITestExecutor.RunTests(IEnumerable<string> sources, IRunContext runContext, ITestExecutionRecorder testLog)
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            //var package = new TestPackage("", SanitizeSources(sources));
            //var listener = new NUnitEventListener(testLog, null, null);

            //if (runner.Load(package))
            //    runner.Run(listener);

            foreach (var source in SanitizeSources(sources))
            {
                //RunAssembly(source, testLog, null);
                RunAssembly(source, frameworkHandle, null);
            }
        }

        //void ITestExecutor.RunTests(IEnumerable<TestCase> selectedTests, IRunContext runContext, ITestExecutionRecorder testLog)
        public void RunTests(IEnumerable<TestCase> selectedTests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            var assemblyGroups = selectedTests.GroupBy(tc => tc.Source);

            foreach (var assemblyGroup in assemblyGroups)
            {
                //var selectedTestsMap = assemblyGroup.ToDictionary(tc => tc.Name);
                var selectedTestsMap = assemblyGroup.ToDictionary(tc => tc.FullyQualifiedName);
                //RunAssembly(assemblyGroup.Key, testLog, selectedTestsMap);
                RunAssembly(assemblyGroup.Key, frameworkHandle, selectedTestsMap);
            }
        }

        private void RunAssembly(string assemblyName, ITestExecutionRecorder testLog, Dictionary<string, TestCase> testCaseMap)
        {
            try
            {
                this.runner = new TestDomain();
                TestPackage package = new TestPackage(assemblyName);
                runner.Load(package);


                var listener = new NUnitEventListener(testLog, testCaseMap, assemblyName);
                var filter = testCaseMap != null
                    ? new TestRunFilter(testCaseMap.Keys.ToList())
                    : TestFilter.Empty;

                try
                {
                    runner.Run(listener, filter, false, LoggingThreshold.Off);
                }
                catch (NullReferenceException)
                {
                    // this happens during the run when CancelRun is called.
                }
                finally
                {
                    listener.Dispose();
                    runner.Unload();
                }
            }
            catch (System.BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
            }
            catch (Exception e)
            {
                testLog.SendMessage(TestMessageLevel.Error, e.Message);
            }
        }

        void ITestExecutor.Cancel()
        {
            if (runner != null && runner.Running)
                runner.CancelRun();
        }

        #endregion
    }
}