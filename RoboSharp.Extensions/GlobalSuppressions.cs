// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Multi-Target")]
[assembly: SuppressMessage("Performance", "IDE0090:Use 'new(...)'", Justification = "Required by .NetStandard Target")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "Uses Appropriate Constructor", Scope = "member", Target = "~M:RoboSharp.Extensions.RoboMover.RunAsRoboMover(System.String,System.String,System.String)~System.Threading.Tasks.Task{RoboSharp.Results.RoboCopyResults}")]
[assembly: SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "Unnecessary. Digging into code reveals it creates a new instance even though tooltip says otherwise. (its the same thing with extra steps)")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Slice expression can not be simplified due to Net48 compatibility")]
