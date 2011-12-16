NodeRole is the way [smarx](http://blog.smarx.com) deploys node apps to Windows Azure. Note that these days, there's (built-in support for running Node.js in Windows Azure)[http://www.windowsazure.com/en-us/develop/nodejs/], which you may prefer to use.

The NodeRole uses [iisnode](https://github.com/tjanczuk/iisnode) and [Windows native builds of node.exe](http://nodejs.org/#download).

The code works by downloading prerequisites (node, the iisnode module, and the C++ runtime required) at runtime, installing them in the VMs in Windows Azure, and then synchronizing code from either a git repository or a blob container. This way, changes can be made rapidly without having to redeploy. Dependencies are installed by running [npm](npmjs.org), the Node package manager.

To use it, do the following:

1. Create an app with a server.js in it. The app must listen on the port specified by the `PORT` environment variable (e.g. `listen(process.env.PORT || 3000)`, which will default to port 3000 if simply being run from the command line).
2. Push that code to a public git repository or to a container within Windows Azure blob storage.
3. Build NodeRole (by running `build` from the command line).
4. Configure the application (by editing `*.cscfg`) with either a GitUrl (full URL to your git repository) or the pair of DataConnectionString and ContainerName. You *can* specify both a git URL and a blob container, and both will be sync'd to the same directory.
5. Run the app locally with the Windows Azure compute emulator with `run` from the command line, making sure that you've modified `ServiceConfiguration.Local.cscfg`, which is used when running locally.
6. Deploy the app to Windows Azure, using `NodeRole.cspkg` and `ServiceConfiguration.Cloud.cscfg` from `noderole\bin\release\app.publish`, with appropriate settings in the `.cscfg` file.

As an alternative to downloading and building yourself, you can click on "downloads" above and download a pre-built package and deploy that. (You can still modify `*.cscfg` to use your own app code.)

For example apps, take a look at [twoenglishes](https://github.com/smarx/twoenglishes) and [smarxchat](https://github.com/smarx/smarxchat), both of which are running in Windows Azure using this package.