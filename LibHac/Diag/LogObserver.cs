﻿using LibHac.Common;
using LibHac.Diag.Impl;

namespace LibHac.Diag
{
    public delegate void LogObserver(ref readonly LogMetaData metaData, ref readonly LogBody body, object arguments);

    internal struct LogObserverGlobals
    {
        public LogObserverHolder DefaultLogObserverHolder;

        public nint ManagerGuard;
        public LogObserverManager Manager;
    }

    public class LogObserverHolder
    {
        internal LogObserver Observer;
        internal bool IsRegistered;
        internal object Arguments;
    }

    public static class LogObserverFuncs
    {
        public static void InitializeLogObserverHolder(this DiagClient diag, ref LogObserverHolder holder,
            LogObserver observer, object arguments)
        {
            holder.Observer = observer;
            holder.IsRegistered = false;
            holder.Arguments = arguments;
        }

        public static void RegisterLogObserver(this DiagClient diag, LogObserverHolder observerHolder)
        {
            diag.Impl.GetLogObserverManager().RegisterObserver(observerHolder);
        }

        public static void UnregisterLogObserver(this DiagClient diag, LogObserverHolder observerHolder)
        {
            diag.Impl.GetLogObserverManager().UnregisterObserver(observerHolder);
        }

        private static void TentativeDefaultLogObserver(ref readonly LogMetaData metaData, ref readonly LogBody body,
            object arguments)
        {

        }

        private static LogObserverManager GetLogObserverManager(this DiagClientImpl diag)
        {
            ref LogObserverGlobals g = ref diag.Globals.LogObserver;
            using var guard = new InitializationGuard(ref g.ManagerGuard, diag.Globals.InitMutex);

            if (!guard.IsInitialized)
            {
                g.Manager = new LogObserverManager(diag.Hos);
                g.DefaultLogObserverHolder = new LogObserverHolder();
                diag.Diag.InitializeLogObserverHolder(ref g.DefaultLogObserverHolder, TentativeDefaultLogObserver, null);
                g.Manager.RegisterObserver(g.DefaultLogObserverHolder);
            }

            return g.Manager;
        }

        internal static void CallAllLogObserver(this DiagClientImpl diag, ref readonly LogMetaData metaData,
            ref readonly LogBody body)
        {
            var context = new LogObserverContext
            {
                MetaData = metaData,
                Body = body
            };

            LogObserverManager manager = diag.GetLogObserverManager();

            manager.InvokeAllObserver(in context, InvokeFunction);

            static void InvokeFunction(ref LogObserverHolder holder, ref readonly LogObserverContext item)
            {
                holder.Observer(in item.MetaData, in item.Body, holder.Arguments);
            }
        }

        internal static void ReplaceDefaultLogObserver(this DiagClientImpl diag, LogObserver observer)
        {
            ref LogObserverGlobals g = ref diag.Globals.LogObserver;

            diag.Diag.UnregisterLogObserver(g.DefaultLogObserverHolder);
            diag.Diag.InitializeLogObserverHolder(ref g.DefaultLogObserverHolder, observer, null);
            diag.Diag.RegisterLogObserver(g.DefaultLogObserverHolder);
        }

        internal static void ResetDefaultLogObserver(this DiagClientImpl diag)
        {
            ref LogObserverGlobals g = ref diag.Globals.LogObserver;

            diag.Diag.UnregisterLogObserver(g.DefaultLogObserverHolder);
            diag.Diag.InitializeLogObserverHolder(ref g.DefaultLogObserverHolder, TentativeDefaultLogObserver, null);
            diag.Diag.RegisterLogObserver(g.DefaultLogObserverHolder);
        }
    }
}

namespace LibHac.Diag.Impl
{
    // Todo: Use ref fields once C# 12? is released
    internal ref struct LogObserverContext
    {
        public LogMetaData MetaData;
        public LogBody Body;
    }
}