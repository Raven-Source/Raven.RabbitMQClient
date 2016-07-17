#特性列表
- 消息行为配置
- 连接字符串动态更新
- 同应用所有节点接收消息

##消息行为配置
服务器、队列、路由、生产特性、消费特性都包含在配置文件中。
```
  <configSections>
    <section name="ravenRabbitMQ" type="Raven.Message.RabbitMQ.Configuration.ClientConfiguration, Raven.Message.RabbitMQ"/>
  </configSections>
  <!--
  客户端配置
  logType，日志实现类
  serializerType，序列化方式，默认为MsgPack，可填Jil、MsgPack、Protobuf、NewtonsoftJson、NewtonsoftBson
  -->
  <ravenRabbitMQ logType="Raven.Message.RabbitMQ.Demo.Log,Raven.Message.RabbitMQ.Demo" serializerType="NewtonsoftJson">
    <brokers>
      <!--name为服务器标识，uri为服务器连接字符串-->
      <broker name="localhost" uri="amqp://127.0.0.1">
        <queues>
          <!--
            队列配置
            name，队列名，必填
            durable，队列是否长期保留，默认false
            autoDelete，队列是否自动删除，默认false
            maxPriority，最大优先级，最大10
            expiration，消息过期时间，单位毫秒
            maxLength，最多存储多少消息
            redeclareWhenFailed，当队列声明失败时是否删除并重新创建队列，默认false
            bindToExchange，绑定到路由
            bindMessageKeyPattern，绑定路由关键字
            serializerType，序列化方式，如果配置此项将覆盖客户端配置中的序列化类型
            -->
          <queue name="testqueue" durable="true" autoDelete="false" maxPriority="5" expiration="1000" maxLength="100" redeclareWhenFailed="false" bindToExchange="exchange1" bindMessageKeyPattern="test" serializerType="NewtonsoftJson">
            <!--
            队列生产配置
            sendConfirm，是否启用发送确认，默认false
            sendConfirmTimeout，发送确认超时时间，单位毫秒，默认100
            messagePersistent，消息是否持久化，默认false
            maxWorker，最大发送线程数，默认1
            -->
            <producer sendConfirm="false" sendConfirmTimeout="100" messagePersistent="false" maxWorker="1"></producer>
            <!--
            队列消费配置
            consumeConfirm，是否启用消费确认，默认false
            maxWorker， 最多同时处理消息，默认10
            -->
            <consumer consumeConfirm="false" maxWorker="10"></consumer>
          </queue>
        </queues>
        <exchanges>
          <!--
          路由器配置
          name，路由器名字
          exchangeType，路由器类型，默认为topic，可填
                        fanout，所有消息会被绑定队列消费
                        headers，多条件匹配规则，客户端方法暂不支持
                        direct，消息关键字完全匹配
                        topic，消息关键字模式匹配
          durable，路由器是否长期保留，默认false
          autoDelete，路由器是否自动删除，默认false
          serializerType，序列化方式，如果配置此项将覆盖客户端配置中的序列化类型
          -->
          <exchange name="testexchange" exchangeType="topic" durable="true" autoDelete="false" serializerType="NewtonsoftJson">
            <!--
            路由生产配置
            sendConfirm，是否启用发送确认，默认false
            sendConfirmTimeout，发送确认超时时间，单位毫秒，默认100
            messagePersistent，消息是否持久化，默认false
            maxWorker，最大发送线程数，默认1
            -->
            <producer sendConfirm="false" sendConfirmTimeout="100" messagePersistent="false" maxWorker="1"></producer>
          </exchange>
        </exchanges>
      </broker>
    </brokers>
  </ravenRabbitMQ>
```
##连接字符串动态更新
服务器连接字符串变更时，无需重启服务，动态更新连接，并安全退出原有连接。
实现客户端提供的IBrokerWatcher接口
```
    public class MQBrokerWatcher : IBrokerWatcher
    {
        public event EventHandler<BrokerChangeEventArg> BrokerUriChanged;

        public string GetBrokerUri(string brokerName)
        {
            return ConfigContainer.Instance.Get(brokerName);
        }
    }
```
在初始化方法中注入IBrokerWatcher对象实例，连接字符串将通过此实例查询（配置文件中的字符串失效）
```
            Config.MQBrokerWatcher watcher = new Config.MQBrokerWatcher();
            Client.Init(watcher);
```
##同应用所有节点接收消息
通常情况一条消息只会发送给一个应用的一个节点，但是某些特殊场景（例如本地缓存更新）需要所有节点接收消息。
在队列名中配置IP占位符或者进程Id占位符，创建队列时将动态替换占位符。
```
<queue name="messagecenter_configupdate{_IP}" durable="true"></queue>
```
订阅消息
```
            string queue = "messagecenter_configupdate{_IP}";
            _client.Consumer.Subscribe<string>("testexchange", queue, null, callback);
```
