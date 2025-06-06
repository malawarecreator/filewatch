# filewatch
just a file watcher. to configure, download a release, unzip it, and set the path in `filewatch.exe.config`<br>
to start it: run `sc create FileWatcher binPath="path-to-filewatch.exe" start=auto` + `net start FileWatcher`<br>
to check logs, go to `eventvwr`->windows logs->Applications<br>
this was a shitty ass project, js for learning.
