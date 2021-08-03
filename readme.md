- [RabbitCLI](#rabbitcli)
  - [Download latest release](#download-latest-release)
  - [Commands](#commands)
    - [Configuration](#configuration)
      - [Command: `add-config`](#command-add-config)
      - [Configuration Storage](#configuration-storage)
      - [Command: `get-configs`](#command-get-configs)
      - [Command: `update-config`](#command-update-config)
    - [Queues](#queues)
      - [Command: `get-queues`](#command-get-queues)
    - [Messages](#messages)
      - [Command: `get-messages`](#command-get-messages)
      - [Filter possibilites](#filter-possibilites)
      - [Live-Streaming messages](#live-streaming-messages)
      - [Command: `move-messages`](#command-move-messages)
      - [Command: `purge-messages`](#command-purge-messages)

# RabbitCLI
## Download latest release
See [release branch](https://github.com/bkrieger1991/rabbittools/tree/releases) for all available releases.
For installation just unzip the downloaded archive and either execute the `rabbitcli.exe` directly, or run the `install.ps1` script to copy the RabbitMQ CLI into `C:\Users\<YourName>\AppData\Local\RabbitCLI\rabbitcli.exe` and adding this path to your `PATH` environment variable.

## Development
Just open the solution in VisualStudio. It's built with the VS 2019 Community edition, there is nothing special you have to do.
To later execute and debug commands you have to provide debug-arguments.
Otherwise, just run the terminal in the build output folder, to refer to the `rabbitcli.exe`.

## Create a release
Just execute the `publish-app-win-x64.ps1`. It will build, publish a self-contained single-file app and pack an archive also using the `install.ps1`.
This archive can then be used to distribute the release.

## Commands
### Configuration
For management of different configurations, you can add, change and delete configurations.
A configuration contains all information to establish a connection to a RabbitMQ host.
#### Command: `add-config`
To create a new configuration, just call the `add-config` command and provide all required options.

|Option|Example Value|Desription|
|---|---|---|
|`--username`|*guest*|A username, permitted to access RabbitMQ Management API and perform AMQP Actions|
|`--password`|*guest*|The password of your user|
|`--vhost`|"*/*" or *youHost*|The virtualhost you want to connect to. If you want to manage different virtual-hosts, you have to create different configurations.|
|`--web`|*http://localhost:15672*|The address of your management api (if you have enabled SSL, use `https`)|
|`--amqp`|*amqp://localhost:5672*|The address for AMQP connections|
|`--name`|*myConfig*|Name of you configuration.|

The **default** configurtaion will be created if you don't provide a name for a certain configuration.
The default configuration entry always gets loaded, if you don't provide a configuration-name in your commands (using the `-c` or `--config` option)

**Example of creating a default config**
```
rabbitcli add-config \
    --username guest \
    --password guest \
    --vhost "/" \
    --web http://localhost:15672 \
    --amqp http://localhost:5672
```
**Example of creating a configuration with name `configname`**
```
rabbitcli add-config --name configname \
    --username guest \
    --password guest \
    --vhost "/" \
    --web http://localhost:15672 \
    --amqp http://localhost:5672
```
#### Configuration Storage
The configuration is stored in a `json` file in your local user-profile folder (on windows systems: `C:\users\<your-name>\rabbitcli.json`)

The configuration looks like:
```json
{
  "default": "opzZeXICsX/4BzbUHrXnU...",
  "customName": "S2mFAY0FnuoVGZl3Zlpyr..."
}
```
The content of a configuration is stored encrypted and gets only decrypted when using the command. So your credentials are not persisted in plain-text.

#### Command: `get-configs`
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

#### Command: `update-config`
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

### Queues
#### Command: `get-queues`
The `get-queues` command has following options:
|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use. Defaults to `default` config.|
|`--queue`|*"my.queue.name"*|Show details of a queue providing it's name|
|`--qid`|*1098535bebc1*|Show details of a queue providing it's ID (only generated by rabbitcli)|
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

### Messages
#### Command: `get-messages`
This are the options available for the `get-messages` command

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use. Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to fetch messages from (alternative to `--queue`|
|`--queue`|*My.Queue.Name*|The name of the queue you want to fetch messages from (alternative to `--qid`)
|`--headers`||Provide this option if you want to show headers in the result view of messages|
|`--limit`|*10*|Limit the result of messages to a defined amount|
|`--filter`|*Any value*|Filter a string within the message (also the message-body). See filter possibilites for further explanation|
|`--json`||Output messages as json array, to better analyze contents|
|`--hash`||Provide a message-hash (shown in result list view) to fetch details about a single message|
|`--body`||Output body content of a single message. Only works in combination with `--hash` option|
|`--live-view`||**EXPERIMENTAL**: Read more about this in below section "Live-Streaming messages"|

#### Filter possibilites
Here are some filter examples you can use:

|Filter-Value|Result|
|---|---|
|`--filter "My Text"`|`My Text` is searched within the body of a message|
|`--filter "properties:SomeValue"`|`SomeValue` is searched in the properties: `AppId`, `ClusterId`, `ContentEncoding`, `ContentType`, `CorrelationId`, `Expiration`, `MessageId`, `ReplyTo` of a message|
|`--filter "headers:SomeValue"`|`SomeValue` is searched in all headers of a message|

#### Live-Streaming messages
The live-streaming feature is currently in the experimental state, cause it was not yet tested until it's bullet-proof and it may lead to unexpected behaviour.

**It's not recommended you use this on your production environment.**

With this feature you can live-stream every message that will be passed to a queue from it's exchange-bindings.

**How that works**

![Live-View Picture](docs/img/live-view.png)

The command creates a new temporary queue that will get each exchange-binding of the source-queue. So every message delivered to the original queue, will also be dropped into our temporary queue.

After that, every message is fetched from that temporary queue and will get directly aknowledged and deleted.

You can see a live-stream of messages in your console. This even works using a filter.

#### Command: `move-messages`
To move messages between queues, you can use the `move-messages` command. Here are the options you can provide:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use. Defaults to `default` config.|
|`--from-qid`|*1098535bebc1*|The ID of the queue you want to move messages from (alternative to `--from`)|
|`--from`|*My.Queue.Name*|The name of the queue you want to move messages from (alternative to `--from-qid`)
|`--to-qid`|*1098535bebc1*|The ID of the queue you want to move messages to (alternative to `--to`). Not working with `--new`|
|`--to`|*My.Queue.Name*|The name of the queue you want to move messages to (alternative to `--to-qid`)
|`--new`||When using this option, the queue defined in `--to` will be created first.|
|`--copy`||Messages are not getting removed from the source-queue in `--from`|
|`--limit`|*10*|Limit the amount of messages getting moved|
|`--filter`|*Any value*|Filter a string within the message (also the message-body). See filter possibilites for further explanation|

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

#### Command: `purge-messages`
You can purge messages from queues using the benefits known from other commands: filter and adressing single messages. Here are the options of the command:

|Option|Example Value|Description|
|---|---|---|
|`-c` or `--config`|*myConfig*|The configuration you want to use. Defaults to `default` config.|
|`--qid`|*1098535bebc1*|The ID of the queue you want to purge messages from (alternative to `--queue`)|
|`--queue`|*My.Queue.Name*|The name of the queue you want to purge messages from (alternative to `--qid`)|
|`--filter`|*Any value*|Filter a string within the message (also the message-body). See filter possibilites for further explanation|
|`--hash`||Provide a message-hash (shown in result list view) to purge only that message|

**WARNING ABOUT USING `--hash`**: The hash of a message is calculated using the body and some properties, available in native RabbitMQ. If the body and the values of those message-properties are **identical** to another message, the hashes also equals.

**Purging a message using the `--hash` option will purge all messages where the calculated hash matches**