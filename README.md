# Asynchronous Programming

## Error handling

* So what is up with `async void` methods and error handling?
* How does this impact event handlers?
* Synchronous vs. asynchronous throwing
* Awaiting faulted tasks

## Execution (sync vs. actually async)

* Tasks that don't lean on an asynchrony source are executed synchronously
* Not awaiting tasks (that only produce results or trigger side effects) is bad because there is no information about completion of the query/command, so you can't schedule more stuff to happen afterwards.
* `async void` is a killer of asyncrony (not necessarily a bad thing). If your `Task`/`Task<T>` does actually wrap an asynchrony source (of which there are only a few: `Task.Run` (a.k.a the `ThreadPool`), any async I/O implementation from the framework (like `FileStream.ReadAsync`), a `TaskCompletionSource<T>` that wraps a new thread (STA or otherwise)... and that's about all I can come up with), then having it awaited in an `async void` function will not register a continuation (no new Task will be created to be awaited), and thus all statements that follow the `async void` method invocation will be executed immediately after said invocation (as opposed to being captured in a continuation).

## Progress reporting

* You can report progress for a `Task.WhenAll` task, by injecting individual `IProgress<T>` in each task, have them use the same reporting method (that needs to be synchronized) and divide the cummulated progress percentage by the count of tasks.