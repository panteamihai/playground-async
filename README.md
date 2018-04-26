# Asynchronous Programming

## Error handling

* So what is up with `async void` methods and error handling?
* How does this impact event handlers?
* Synchronous vs. asynchronous throwing
* Awaiting faulted tasks
* Tasks that don't lean on an asynchrony source are executed synchronously
* Not awating tasks (that only produce results or trigger side effects) is bad because there is no information about completion of the query/command, so you can't schedule more stuff to happen afterwards.