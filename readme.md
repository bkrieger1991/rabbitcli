[![.NET](https://github.com/bkrieger1991/rabbittools/actions/workflows/dotnet.yml/badge.svg)](https://github.com/bkrieger1991/rabbittools/actions/workflows/dotnet.yml)
[![Publish Windows x64 ZIP](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish-win-x64.yml/badge.svg)](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish-win-x64.yml)
[![Publish OSX arm64-x64 Universal Bundle](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish-osx-universal.yml/badge.svg)](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish-osx-universal.yml)
[![Publish Docker Image](https://github.com/bkrieger1991/rabbitcli/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/bkrieger1991/rabbitcli/actions/workflows/docker-publish.yml)

# What is RabbitCLI?
Rabbit CLI should help you to perform tasks with a RabbitMQ instance, you can't do with the Management UI addon.

This CLI tool helps fetching messages with extended filter functionality, editing messages in queues, moving messages from queue A to B (even with filter functionality), purging messages with a filter applied and more.

It allows you to configure more than one instance, to e.g. perform actions on your local development instance as well on your staging or productive environment.

It's written in C# .NET 6.0 and available for download in the branch `releases`.

- [What is RabbitCLI?](#what-is-rabbitcli)
- [RabbitMQ HTTP-Proxy Docker-Image](#rabbitmq-http-proxy-docker-image)
- [Download latest CLI-Tool release](#download-latest-cli-tool-release)
    - [See release branch for all available releases](#see-release-branch-for-all-available-releases)
  - [Installation](#installation)
- [Command structure](#commands)
- [Contribution & Development](#contribution--development)
  - [Pull request into `master`](#pull-request-into-master)
  - [Create a release](#create-a-release)

# RabbitMQ HTTP-Proxy Docker-Image
There is a docker-image in the official docker-hub, that enables you to integrate the http-proxy functionality (that is also available using the CLI) in your hosting-environment.

[Read the documentation on docker-hub for all details: **https://hub.docker.com/r/flux1991/rabbitmq-http-proxy**](https://hub.docker.com/r/flux1991/rabbitmq-http-proxy)

# Download latest CLI-Tool release
### See [release branch](https://github.com/bkrieger1991/rabbittools/tree/releases) for all available releases

## Installation
Just unzip the downloaded archive and either invoke a command (described below) on the `rabbitcli.exe` directly or run the `install.ps1` script to copy the RabbitMQ CLI into `C:\Users\<YourName>\AppData\Local\RabbitCLI\rabbitcli.exe` and adding this path to your user's `PATH` environment variable.

# Commands
General Usage: **`rabbitcli <resource> <action> <options>`**<br>
## Structure of resources/actions
|**rabbitcli**|`<resource>`|`<action>`|`<options>`|
|--|--|--|--|
||`config`|`add`|`--amqp` *(required)*, `--web` *(required)*, `--name`, `--ignore-invalid-cert`, `--amqps-tls-version`, `--amqps-tls-server`
|||`get`|`--name`
|||`delete`|`--name` *(required)*
|||`edit`|`--name` *(required)*, `--amqp`, `--web`, `--ignore-invalid-cert`, `--amqps-tls-version`, `--amqps-tls-server`
|||`use`|`--name` *(required)*
||`property`|`get`||
|||`set`|`--name` *(required)*, `--value` *(required)*
||`queue`|`get`|`--queue`, `--qid`, `--sort`, `--desc`, `--limit`, `--filter`, `--exclude`
||`message`|`get`|`--qid` *(required)*, `--queue` *(required)*, `--hash`, `--dump`, `--dump-metadata`, `--body`, `--headers`, `--json`, `--live-view`
|||`purge`|`--qid` *(required)*, `--queue` *(required)*, `--hash`, `--filter`
|||`restore`|`--qid` *(required)*, `--queue` *(required)*, `--dump` *(required)*, `--content-type`, `--routing-key`
|||`move`|`--from-id` *(required)*, `--from` *(required)*, `--to-id` *(required)*, `--to` *(required)*, `--filter`
|||`edit`|`--qid` *(required)*, `--hash` *(required)*
## Configuration
For management of different configurations, you can add, change and delete configurations.
A configuration contains all information to establish a connection to a RabbitMQ host.
### Command: `config add`
To create a new configuration, just call the `add-config` command and provide all required options.

|Option|Example Value|Desription|
|---|---|---|
|`--amqp`|*guest*|The amqp-connectionstring. If username and password are equal to web credentials, you can leave them blank.<br/>Examples:<br/>`amqp://guest@guest:localhost:5672/vhost`<br/>`amqps://your-amqp.host.com:5671/vhost`|
|`--web`|*guest*|The web-connectionstring. If username and password are equal to amqp credentials, you can leave them blank. <br/>Examples:<br/>`http://localhost:15672`<br/>`https://guest:guest@your-other-host.com:443`|
|`--name`|*myconfig*|The name to refer to this configuration|
|`--ignore-invalid-cert`||The hostname used for amqp and management api calls|
|`--amqps-tls-version`|*5672*|The AMQP port (default: 5672)|
|`--amqps-tls-server`|*15672*|The management API port (default: 15672)|

**Example of creating a config**
```
rabbitcli config add \ 
    --amqp amqp://guest:guest@localhost:5672/ \
    --web http://localhost:15672
```
> If you don't provide `--name` attribute, the name "default" is chosen.

**Example of creating a configuration with name `configname`**
```
rabbitcli config add \ 
    --amqp amqp://guest:guest@localhost:5672/ \
    --web http://localhost:15672 \
    --name configname
```
**Example of creating a configuration with different management API credentials**
```
rabbitcli config add \ 
    --amqp amqp://guest:guest@localhost:5672/ \
    --web http://otherguest:otherguest@localhost:15672 \
```

### Configuration Storage
The configuration is stored in a `json` file in your local user-profile folder (on windows systems: `C:\users\<your-name>\rabbitcli.json`)

The configuration looks like:
```json
{
  "default": "opzZeXICsX/4BzbUHrXnU...",
  "customName": "S2mFAY0FnuoVGZl3Zlpyr..."
}
```
The content of a configuration is stored encrypted and gets only decrypted when using the command. So your credentials are not persisted in plain-text.

### Command: `config get`
Either get a list of all configs or output a single config when providing the `--name` option.

|Option|Example Value|Desription|
|---|---|---|
|`--name`|*myConfig*|Name of you configuration you want to view in detail (optional)|

**Get all configurations existing**
```
rabbitcli config get
```

**Get `default` configuration in detail**
```
rabbitcli config get --name default
```
Result Example:
```json
{
  "Name": "black",
  "Amqp": {
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "ierp",
    "Port": 5672,
    "Hostname": "amqps-rabbitmq.black.igus.com",
    "IsAmqps": false,
    "Unsecure": true,
    "TlsVersion": null,
    "TlsServerName": null
  },
  "Web": {
    "Username": "guest",
    "Password": "guest",
    "Port": 80,
    "Hostname": "mgmt-rabbitmq.black.igus.com",
    "Ssl": false,
    "Unsecure": true
  }
}

CLI-Parameters for edit:
rabbitcli config edit --name black --amqp amqp://guest:guest@localhost:5672/ierp --web http://guest:guest@mgmt-rabbitmq.black.igus.com/ --ignore-invalid-cert
```
The `config get` command will output the decrypted configuration with the password, so you can check what value is in there.

>You may notice, that a template for editing the configuration will be printed. Just to make things easier.

### Command: `config edit`
This command helps you change values within an existing configuration.
You have to provide the same arguments as when creating a configuration.

You can copy & paste an edit-command when executing `rabbitcli config get --name myconfig`.

Example:
```
rabbitcli config edit \
  --name black \
  --amqp amqp://guest:guest@localhost:5672/ierp \
  --web http://guest:guest@mgmt-rabbitmq.black.igus.com/ \
  --ignore-invalid-cert
```

### Command: `config delete`
If you want to delete a configuration, simply provide your name in `--name` option:
```
rabbitcli config delete --name myconfig
```

### Command: `config use`
You can configure any of your configurations as "default". <br/>
This way, it gets used in every other command (like e.g. `rabbitcli queue get`) as long there is no `-c` or `--config` specified.

Example: 
```
rabbitcli config use --name myconfig
```

### Command: `property get`
This command shows a list of configurable properties. Properties are meant as global configuration keys, that control the behavior of the rabbitcli tool.

Example of getting all properties:
```
rabbitcli property get
```

### Command: `property set`
This command can get properties, to configure the general behaviour of the rabbitcli.

List of options:

|Option|Example Value|Desription|
|---|---|---|
|`--name`|*propertyname*|Outputs a list of all properties available and the current value|
|`--value`|*value*|Provide a property-name to set a new value|

**Example of configuring an alternative text editor for editing messages**
```
rabbitcli property set --name "texteditorpath" --value "code"
```

## Queues
### Command: `queues get`
The `queues get` command has following options:
|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--queue`|*"my.queue.name"*|Show details of a queue providing it's name|
|`--qid`|*1098535bebc1*|Show details of a queue providing it's ID<br>(only generated by rabbitcli)|
|`--sort`|*messages*|Sort list of queues by a certain property|
|`--desc`||Used together with `--sort`, orders the results descending|
|`--limit`|*10*|Limit your list of queues to an amount|
|`--filter`|*"some text"*|Provide a text, that will be searched within the queue-name|

Example Queue-Object when displaying details:

```json
{
  "Name": "Queue 1",
  "Id": "1098535bebc1",
  "Consumers": 0,
  "Messages": 15,
  "MessagesReady": 15,
  "MessagesUnacknowledged": 0,
  "AutoDelete": false,
  "Durable": true,
  "Node": "rabbit@rabbitmq",
  "Policy": null,
  "Vhost": "/",
  "Memory": 30980
}
```

**Example of listing top 10 queues ordered by message-count:**
```
rabbitcli queues get --sort messages --desc --limit 10
```

**Example of showing detials about a certain queue:**
```
rabbitcli queues get --qid 1098535bebc1
```

## Messages
### Command: `message get`
This are the options available for the `messages get` command

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to fetch messages from<br>(alternative to `--queue`|
|`--queue`|*My.Queue.Name*|The name of the queue you want to fetch messages from<br>(alternative to `--qid`)
|`--headers`||Provide this option if you want to show headers in the <br>result view of messages|
|`--limit`|*10*|Limit the result of messages to a defined amount|
|`--filter`|*Any value*|Filter a string within the message (also the message-body).<br>See filter possibilites for further explanation|
|`--json`||Output messages as json array, to better analyze contents|
|`--hash`||Provide a message-hash (shown in result list view) to fetch details<br>about a single message|
|`--body`||Output body content of a single message. <br>Only works in combination with `--hash` option|
|`--live-view`||**EXPERIMENTAL**: Read more about this in below section<br>[Live-Streaming messages](#live-streaming-messages)|
|`--dump`|&lt;Directory&gt;|Stores the message data for each message into the given directory. For restore command it defines the directory to restore messages from.|
|`--dump-metadata`||Set option to get a second file <br>(*.meta.json)beside your message-content <br>with all meta-data of the message|

### Filter possibilites
Here are some filter examples you can use:

|Filter-Value|Result|
|---|---|
|`--filter "My Text"`|`My Text` is searched within the body of a message|
|`--filter "properties:SomeValue"`|`SomeValue` is searched in the properties of a message:<br>`AppId`, `ClusterId`, `ContentEncoding`, `ContentType`,<br>`CorrelationId`, `Expiration`, `MessageId`, `ReplyTo`|
|`--filter "headers:SomeValue"`|`SomeValue` is searched in all headers of a message|

### Live-Streaming messages
The live-streaming feature is currently in the experimental state, cause it was not yet tested until it's bullet-proof and it may lead to unexpected behaviour.

**It's not recommended you use this on your production environment.**

With this feature you can live-stream every message that will be passed to a queue from it's exchange-bindings.

**How that works**

![Live-View Picture](docs/img/live-view.png)

The command creates a new temporary queue that will get each exchange-binding of the source-queue. So every message delivered to the original queue, will also be dropped into our temporary queue.

After that, every message is fetched from that temporary queue and will get directly aknowledged and deleted.

You can see a live-stream of messages in your console. This even works using a filter.

### Command: `message move`
To move messages between queues, you can use the `message move` command. Here are the options you can provide:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--from-qid`|*1098535bebc1*|The ID of the queue you want to move messages from<br>(alternative to `--from`)|
|`--from`|*My.Queue.Name*|The name of the queue you want to move messages from<br>(alternative to `--from-qid`)
|`--to-qid`|*1098535bebc1*|The ID of the queue you want to move messages to<br>(alternative to `--to`). Not working with `--new`|
|`--to`|*My.Queue.Name*|The name of the queue you want to move messages to<br>(alternative to `--to-qid`)
|`--new`||When using this option, the queue defined in<br>`--to` will be created first.|
|`--copy`||Messages are not getting removed from <br>the source-queue in `--from`|
|`--limit`|*10*|Limit the amount of messages getting moved|
|`--filter`|*Any value*|Filter a string within the message (also the message-body).<br>See filter possibilites for further explanation|

**Move messages from one queue to another**
```
rabbitcli message move \
  --from My.Source.Queue \
  --to-qid 1098535bebc1
```

**Move only messages to a new queue, that match a certain filter**
```
rabbitcli message move \
  --from-qid 1098535bebc1 \
  --to Backup.Queue.Of.1098535bebc1 \
  --new \
  --filter "headers:Some error message"
```

### Command: `message purge`
You can purge messages from queues using the benefits known from other commands: filter and adressing single messages. Here are the options of the command:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to purge messages from<br>(alternative to `--queue`)|
|`--queue`|*My.Queue.Name*|The name of the queue you want to purge messages from<br>(alternative to `--qid`)|
|`--filter`|*Any value*|Filter a string within the message (also the message-body).<br>See filter possibilites for further explanation|
|`--hash`||Provide a message-hash (shown in result list view) to<br>purge only that message|

**WARNING ABOUT USING `--hash`**: The hash of a message is calculated using the body and some properties, available in native RabbitMQ. If the body and the values of those message-properties are **identical** to another message, the hashes also equals.

**Purging a message using the `--hash` option will purge all messages where the calculated hash matches**

### Command: `message edit`
You can edit the **content** of a message in a queue. For this you can use the `message edit` command with following options:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to purge messages from<br>(alternative to `--queue`)|
|`--queue`|*My.Queue.Name*|The name of the queue you want to purge messages from<br>(alternative to `--qid`)|
|`--hash`|*8130d8764f31*|Provide a hash that identifies a message.|

This command will make use of the configured property `TextEditorPath`. The set value tells rabbitcli which editor it should open in order to edit the message-content. The configured default is `notepad`. 

**Edit a message with hash "8130d8764f31" using the default configuration**
```
rabbitcli message edit --queue My.Queue --hash 8130d8764f31
```


If you wish to use another editor, feel free to update the value to use for e.g. VS Code:
```
rabbitcli property set --name texteditorpath --value "code"
```

### Command: `message restore`
Use the `message restore` command to re-publish messages that were previously saved with the `--dump` option back into a queue.

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration to use. Defaults to `default`.| 
|`--qid`|*1098535bebc1*|The id of the queue to restore messages into (alternative to `--queue`).|
|`--queue`|*My.Queue.Name*|The name of the queue to restore messages into (alternative to `--qid`).|
|`--dump`|&lt;Directory&gt;|Directory to read files from. Should contain files produced by `message get --dump`.| 
|`--content-type`|*application/json*|Content type that should be used when publishing all messages. Overwrites value from metadata files.| 
|`--routing-key`|*some.routing.key*|Routing key that should be used when publishing all messages. Overwrites value from metadata files.| 

Note: Each content file may have an accompanying `*-meta.json` file (created with `--dump-metadata`). If present, metadata such as `ContentType`, message properties (e.g. `MessageId`, `AppId`) and `Headers` will be applied when publishing. If `Fields.RoutingKey` exists in the metadata it will be used as the routing key for publishing.

**Example: restore messages from a dump into `My.Queue`**
```
rabbitcli message restore --queue My.Queue --dump C:\temp\mydump --content-type application/json
```

## HTTP-Proxy: Command `proxy`
The `proxy` command provides a kind of HTTP-Proxy to publish messages to your configured RabbitMQ instance using HTTP-Requests.

You can use a tool of your choice that is capable of making (and maybe managing) HTTP-Requests (like [Postman](https://www.postman.com/) or [Thunderclient](https://www.thunderclient.io/) for example).

> This functionality is also available as a docker-image in the official docker-hub to get easily integrated in your existing hosting-environment. See https://hub.docker.com/r/flux1991/rabbitmq-http-proxy for detailed description.

The `proxy` command has following options:

|Option|Default-Value|Description|
|---|---|---|
|`-c` or `--config`|`default`|The configuration you want to use.<br>Defaults to `default` config|
|`--port`|`15673`|Provide an alternative port to start the proxy, <br>if the configured default is already used in your system|
|`--except-headers`|`Content-Length,Host,User-Agent,Accept,Accept-Encoding,Connection,Cache-Control`|Provide a comma-separated list of header-keys you don't want to transfer into the published message-headers|
|`--headless`||Providing this option will turn of all extra-messages in the console and will run a plain web-host|

The `proxy` command starts a web-host on the machine, where it gets executed. Press `CTRL+C` to quit the running web-host.

### Publishing messages

Consider the detailed documentation. Most of it, even when not running the proxy as a docker-container, will also apply to the proxy running in your console, as it is nearly the same: [RabbitMQ HTTP Proxy Docker Image Documentation](src/RabbitMQ.CLI.Proxy/readme.md)

#### Message Headers
Any HTTP Header that is contained in your request and not gets filtered by `--except-headers` filter, and is not prefixed with `RMQ-` will be published as message-header.

# Contribution & Development
Feel free to contribute. Just open the solution in VisualStudio. It's built with the VS 2019 Community edition, there is nothing special you have to do.

To later execute and debug commands you have to provide debug-arguments.
Otherwise, just run the terminal in the build output folder, to refer to the `rabbitcli.exe`.

## Pull request into `master`
The `master` branch is locked and can only be changed using pull-requests.

## Create a release
Creating new release-versions is covered by GitHub Actions which will automatically push new archive-versions to `releases` branch.