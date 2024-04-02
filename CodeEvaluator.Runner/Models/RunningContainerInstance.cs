using Docker.DotNet;

namespace CodeEvaluator.Runner.Models;

public record RunningContainerInstance(string ContainerId, MultiplexedStream ShellStream, DateTime StartTime) { }