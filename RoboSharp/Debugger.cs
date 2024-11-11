using System;
using System.Diagnostics;

namespace RoboSharp
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public sealed class Debugger
    {
        private static readonly Lazy<Debugger> instance = new Lazy<Debugger>(() => new Debugger());

        [DebuggerHidden()]
        private Debugger()
        {

        }

        public static Debugger Instance
        {
            get { return instance.Value; }
        }

        public EventHandler<DebugMessageArgs> DebugMessageEvent;

        public class DebugMessageArgs : EventArgs
        {
            public object Message { get; set; }
            public override string ToString() => Message.ToString();
        }

        [DebuggerHidden()]
        private void RaiseDebugMessageEvent(object message)
        {
            DebugMessageEvent?.Invoke(this, new DebugMessageArgs
            {
                Message = message
            });
        }

        [DebuggerHidden()]
        internal void DebugMessage(object data)
        {
            RaiseDebugMessageEvent(data);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
