# Asynchronous Programming

## Basics - Error handling

* So what is up with `async void` methods and error handling?
* How does this impact event handlers?
* Synchronous vs. asynchronous throwing
* Awaiting faulted tasks

## Basics - Execution (sync vs. actually async)

* Tasks that don't lean on an asynchrony source are executed synchronously.
* Not awaiting tasks (that only produce results or trigger side effects) is bad because there is no information about completion of the query/command, so you can't schedule more stuff to happen afterwards.
* `async void` is a killer of asyncrony (not necessarily a bad thing). If your `Task`/`Task<T>` does actually wrap an asynchrony source (of which there are only a few: `Task.Run` (a.k.a the `ThreadPool`), any async I/O implementation from the framework (like `FileStream.ReadAsync`), a `TaskCompletionSource<T>` that wraps a new thread (STA or otherwise)... and that's about all I can come up with), then having it awaited in an `async void` function will not register a continuation (no new Task will be created to be awaited), and thus all statements that follow the `async void` method invocation will be executed immediately after said invocation (as opposed to being captured in a continuation).

### Note

The following functionalities, captured in the [UsagePatterns project] are all taken and extended from [Liam Westley]'s brilliant NDC London 2014 talk [Async C# 5.0 - Patterns For Real World Use] so props to him for a great presentation and source material, check out his [original implementation]. The [demo album] of the featured band, Silents, must be downloaded in all formats (m4a, flac, mp3, ogg) and placed in the root repo folder inside a folder called media, or the path to the location of the download needs to be adjusted in the [MediaService implementation].

Also note that [clearing the Windows file cache] is essential for running this project under Windows multiple times (for now at least). You could run the solution / exe **as an admin** and make use of the *[Empty Standby List]* tool by placing it in the Utility folder for the task to be automated.

## Progress reporting

> There is one built-in progress reporter: `Progress<T>`. You can either pass an `Action<T>` into the constructor or handle the `ProgressChanged` event. One important aspect of this class is that **it invokes `ProgressChanged` (and the `Action<T>`) in the context in which it was constructed**. So it’s natural to write **UI updates**.

Stephen Cleary - [Reporting Progress from Async Tasks]

* You can report progress for a `Task.WhenAll` task, by injecting individual `IProgress<T>` in each task and have them use the same reporting method. 
* Since the reporting is done on the same thread (the UI thread that constructs the `Progress` objects in our case), there is no need for synchronization, since all calls come in sequentially and are handled synchronously.
* Each individual task will report **[incremental]** updates (how many bytes they managed to read in one pass) and we will cumulate everything in the common reporting action (average the cumulated bytes read by the number of tasks).

## Cancellation
* We introduce the concept of a (sequential) bound command since, for example, the cancellation cannot happen unless downloading is in progress.
* Witness the proper cancellation etiquette that involves throwing an `OperationCanceledException`.
* Catching said exception type for handling the cleanup phase at a task level and also at the central point of aggregation (the common reporting action).

### Move the whole structure of the program to user controls and view models.

## Throttling
* Use a [fixed size buffer] for running a set number of tasks at a time.
* Show the progress by assigning each running task one specific bar to constantly update with new info.
* Allow for cancellation

### Remove duplication in view models by introducing [a base class]

[Reporting Progress from Async Tasks]: <https://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html>
[incremental]: <https://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html>
[clearing the Windows file cache]: <https://stackoverflow.com/questions/478340/clear-file-cache-to-repeat-performance-testing>
[UsagePatterns project]: <https://github.com/panteamihai/workshop-async/tree/master/UsagePatterns>
[Liam Westley]: <https://twitter.com/westleyl>
[Async C# 5.0 - Patterns For Real World Use]: <https://vimeo.com/97337304>
[original implementation]: <https://github.com/westleyl/NDCOslo-AsyncPatterns>
[demo album]: <https://silents.bandcamp.com/>
[MediaService implementation]: <https://github.com/panteamihai/workshop-async/blob/master/UsagePatterns/Services/MediaPathService.cs#L10>
[fixed size buffer]: <https://github.com/panteamihai/workshop-async/blob/master/UsagePatterns/ViewModels/WhenAnyThrottledViewModel.cs#L118>
[Empty Standby List]: <https://wj32.org/wp/software/empty-standby-list/>
[a base class]: <https://github.com/panteamihai/workshop-async/blob/master/UsagePatterns/ViewModels/OperationViewModel.cs>