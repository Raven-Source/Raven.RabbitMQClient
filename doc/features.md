#特性列表
- 消息行为配置
- 连接字符串动态更新
- 同应用所有节点接收消息
- 延迟消息
- 消息重试间隔
- 消费消息跳过重试

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
            messageDelay，消息延迟，单位毫秒
            -->
            <producer sendConfirm="false" sendConfirmTimeout="100" messagePersistent="false" maxWorker="1" messageDelay="1000"></producer>
            <!--
            队列消费配置
            consumeConfirm，是否启用消费确认，默认false
            maxWorker， 最多同时处理消息，默认10
			retryInterval，重试消费间隔，如果不配置则马上重试，单位毫秒，注意此配置必须设置consumeConfirm为true才生效
            skipRetry, 跳过重试消费，默认false
            -->
            <consumer consumeConfirm="false" maxWorker="10" retryInterval="1000" skipRetry="false"></consumer>
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
##延迟消息
某些时候需要延迟收到消息，注意此功能只能针对队列设置延迟时间，而不能每条消息设置不同的延迟时间。  
首先在配置文件中指定队列延迟时间，单位为毫秒
```
    <queue name="DelayQueue" durable="true">
      <producer messageDelay="1000"></producer>
    </queue>
```
调用发送延迟消息方法
```
    bool success = _client.Producer.SendDelay<string>(message, DelayQueue);
```
##消息重试间隔
当消息消费失败后，希望过一段间隔再重新消费，只需要配置一下重试间隔，单位毫秒，注意此配置必须设置consumeConfirm为true才生效
```
    <queue name="DelayRetryQueue" durable="true">
		<consumer consumeConfirm="true" retryInterval="1000"></consumer>
    </queue>
```
##消费消息跳过重试
当消息消费失败后，不希望进行重试，设置skipRetry=true
```
    <queue name="SkipRetryQueue" durable="true">
		<consumer consumeConfirm="true" skipRetry="true"></consumer>
    </queue>
```