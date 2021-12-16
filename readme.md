[![.NET](https://github.com/bkrieger1991/rabbittools/actions/workflows/dotnet.yml/badge.svg)](https://github.com/bkrieger1991/rabbittools/actions/workflows/dotnet.yml)
[![Publish Windows-x64](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish.yml/badge.svg)](https://github.com/bkrieger1991/rabbitcli/actions/workflows/dotnet-publish.yml)

- [What is RabbitCLI?](#what-is-rabbitcli)
- [Download latest release](#download-latest-release)
    - [See release branch for all available releases](#see-release-branch-for-all-available-releases)
  - [Installation](#installation)
- [Contribution & Development](#contribution--development)
  - [Pull request into `master`](#pull-request-into-master)
  - [Create a release](#create-a-release)
- [Commands](#commands)
  - [Configuration](#configuration)
    - [Command: `add-config`](#command-add-config)
    - [Configuration Storage](#configuration-storage)
    - [Command: `get-configs`](#command-get-configs)
    - [Command: `update-config`](#command-update-config)
    - [Command: `config-property`](#command-config-property)
  - [Queues](#queues)
    - [Command: `get-queues`](#command-get-queues)
  - [Messages](#messages)
    - [Command: `get-messages`](#command-get-messages)
    - [Filter possibilites](#filter-possibilites)
    - [Live-Streaming messages](#live-streaming-messages)
    - [Command: `move-messages`](#command-move-messages)
    - [Command: `purge-messages`](#command-purge-messages)
    - [Command: `edit-message`](#command-edit-message)
  - [HTTP-Proxy: Command `proxy`](#http-proxy-command-proxy)

# What is RabbitCLI?
Rabbit CLI should help you to perform tasks with a RabbitMQ instance, you can't do with the Management UI addon.

This CLI tool helps fetching messages with extended filter functionality, editing messages in queues, moving messages from queue A to B (even with filter functionality), purging messages with a filter applied and more.

It allows you to configure more than one instance, to e.g. perform actions on your local development instance as well on your staging or productive environment.

It's written in C# .NET 5.0 and available for download in the branch `releases`.

# Download latest release
### See [release branch](https://github.com/bkrieger1991/rabbittools/tree/releases) for all available releases

## Installation
Just unzip the downloaded archive and either invoke a command (described below) on the `rabbitcli.exe` directly or run the `install.ps1` script to copy the RabbitMQ CLI into `C:\Users\<YourName>\AppData\Local\RabbitCLI\rabbitcli.exe` and adding this path to your user's `PATH` environment variable.

# Contribution & Development
Feel free to contribute. Just open the solution in VisualStudio. It's built with the VS 2019 Community edition, there is nothing special you have to do.

To later execute and debug commands you have to provide debug-arguments.
Otherwise, just run the terminal in the build output folder, to refer to the `rabbitcli.exe`.

## Pull request into `master`
The `master` branch is locked and can only be changed using pull-requests.

## Create a release
Creating new release-versions is covered by GitHub Actions which will automatically push new archive-versions to `releases` branch.

# Commands
## Configuration
For management of different configurations, you can add, change and delete configurations.
A configuration contains all information to establish a connection to a RabbitMQ host.
### Command: `add-config`
To create a new configuration, just call the `add-config` command and provide all required options.

|Option|Example Value|Desription|
|---|---|---|
|`--username`|*guest*|A username, permitted to access<br>RabbitMQ Management API and perform AMQP Actions|
|`--password`|*guest*|The password of your user|
|`--vhost`|"*/*" or *youHost*|The virtualhost you want to connect to.<br>If you want to manage different virtual-hosts,<br>you have to create different configurations.|
|`--host`|*localhost*|The hostname used for amqp and management api calls|
|`--amqp-port`|*5672*|The AMQP port (default: 5672)|
|`--web-port`|*15672*|The management API port (default: 15672)|
|`--web-host`|*my.rabbit.api*|A different hostname for your management api|
|`--ssl`||Provide this argument to enable SSL|
|`--name`|*myConfig*|Name of you configuration.|

The **default** configurtaion will be created if you don't provide a name for a certain configuration.
The default configuration entry always gets loaded, if you don't provide a configuration-name in your commands (using the `-c` or `--config` option)

**Example of creating a default config**
```
rabbitcli add-config \
    --username guest \
    --password guest \
    --vhost "/" \
    --host localhost
```
**Example of creating a configuration with name `configname`**
```
rabbitcli add-config --name configname \
    --username guest \
    --password guest \
    --vhost "/" \
    --host localhost
```
**Example of creating a configuration with a different management API hostname**
```
rabbitcli add-config \
    --username guest \
    --password guest \
    --vhost "/" \
    --host my.rabbit.mq \
    --amqp-port 5672 \
    --web-host api.rabbit.mq \
    --web-port 80 \
    --ssl
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

### Command: `get-configs`
With this command you can simply request what configurations currently exists on your system and output a single configuration. Options for this command:

|Option|Example Value|Desription|
|---|---|---|
|`--name`|*myConfig*|Name of you configuration you want to view in detail|

**Get all configurations existing**
```
rabbitcli get-configs
```

**Get `default` configuration in detail**
```
rabbitcli get-configs --name default
```
Result Example:
```json
{
  "Username": "guest",
  "Password": "guest",
  "VirtualHost": "/",
  "Name": "default",
  "AmqpAddress": "localhost",
  "AmqpPort": 5672,
  "WebInterfaceAddress": "localhost",
  "WebInterfacePort": 15672,
  "Ssl": false
}
```
The `get-configs` command will output the decrypted configuration with the password, so you can check what value is in there.

### Command: `update-config`
This command helps you change single values within a existing configuration.
Providing the `--delete` option, you can delete a configuration.

**Change username in default configuration**
```
rabbitcli update-config --name default --username new-username
```
**Delete a configuration**
```
rabbitcli update-config --name myConfig --delete
```
> When you want to delete your `default` configuration, you have to provide the name explicitly.
> You can then create a new default-config as usual 

### Command: `config-property`
This command can set configuration properties used, to configure the general behaviour of the rabbitcli.

List of options:

|Option|Example Value|Desription|
|---|---|---|
|`--list`||Outputs a list of all properties available and the current value|
|`--set`|*propertyname*|Provide a property-name to set a new value|
|`--value`|*yourValue*|When a property-name was provided, you also need to provide a value|

**Example of configuring an alternative text editor for editing messages**
```
rabbitcli --set "texteditorpath" --value "code"
```

## Queues
### Command: `get-queues`
The `get-queues` command has following options:
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
rabbitcli get-queues --sort messages --desc --limit 10
```

**Example of showing detials about a certain queue:**
```
rabbitcli get-queues --qid 1098535bebc1
```

## Messages
### Command: `get-messages`
This are the options available for the `get-messages` command

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
|`--dump`|&lt;Directory&gt;|Stores the message data for each message into the given directory.|
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

### Command: `move-messages`
To move messages between queues, you can use the `move-messages` command. Here are the options you can provide:

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
rabbitcli move-messages \
  --from My.Source.Queue \
  --to-qid 1098535bebc1
```

**Move only messages to a new queue, that match a certain filter**
```
rabbitcli move-messages \
  --from-qid 1098535bebc1 \
  --to Backup.Queue.Of.1098535bebc1 \
  --new \
  --filter "headers:Some error message"
```

### Command: `purge-messages`
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

### Command: `edit-message`
You can edit the **content** of a message in a queue. For this you can use the `edit-message` command with following options:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use.<br>Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to purge messages from<br>(alternative to `--queue`)|
|`--queue`|*My.Queue.Name*|The name of the queue you want to purge messages from<br>(alternative to `--qid`)|
|`--hash`|*8130d8764f31*|Provide a hash that identifies a message.|

This command will make use of the configured property `TextEditorPath`. The set value tells rabbitcli which editor it should open in order to edit the message-content. The configured default is `notepad`. 

**Edit a message with hash "8130d8764f31" using the default configuration**
```
rabbitcli edit-message --queue My.Queue --hash 8130d8764f31
```


If you wish to use another editor, feel free to update the value to use for e.g. VS Code:
```
rabbitcli --set texteditorpath --value "code"
```

## HTTP-Proxy: Command `proxy`
The `proxy` command provides a kind of HTTP-Proxy to publish messages to your configured RabbitMQ instance using HTTP-Requests.

You can use a tool of your choice that is capable of making (and maybe managing) HTTP-Requests (like [Postman](https://www.postman.com/) or [Thunderclient](https://www.thunderclient.io/) for example).

The `proxy` command has following options:

|Option|Default-Value|Description|
|---|---|---|
|`-c` or `--config`|`default`|The configuration you want to use.<br>Defaults to `default` config|
|`--port`|`15673`|Provide an alternative port to start the proxy, <br>if the configured default is already used in your system|
|`--except-headers`|`Content-Length,Host,User-Agent,Accept,Accept-Encoding,Connection,Cache-Control`|Provide a comma-separated list of header-keys you don't want to transfer into the published message-headers|
|`--headless`||Providing this option will turn of all extra-messages in the console and will run a plain web-host|

The `proxy` command starts a web-host on the machine, where it gets executed. Press `CTRL+C` to quit the running web-host.

### Publishing messages
When the HTTP Proxy is started, you can start making HTTP Requests with a tool of your choice.

#### Body
The body of the HTTP Request is the content of your message. The provided content will be taken without any modification (except reading it as a string with UTF8 encoding).

#### Content-Type
When you provide a `Content-Type` header, it will be set into the RabbitMQ Message Property `content_type`.

#### Other Message Properties
To fill any other property of the published message, just add a header with the prefix `RMQ-`.

For example:

|Header|Value|Result|
|---|---|---|
|`RMQ-Persistent`|`true`|Will set the `Persistent` property of published message to `true`|
|`RMQ-CorrelationId`|`my-correlation-id`|Will set the `CorrelationId` property of published message to `my-correlation-id`|

#### Message Headers
Any HTTP Header that is contained in your request and not gets filtered by `--except-headers` filter, and is not prefixed with `RMQ-` will be published as message-header.