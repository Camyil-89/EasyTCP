![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/424aff49-3db3-4c86-b003-9d288a105bfc)
# EasyTCP
Библиотека для создания простого клиент-серверного приложения. Эта библиотека решает такую проблему как отправка больших пакетов, путем использования своего протокола передачи данных (работает поверх TCP). Большинство библиотек которые есть в интернете для работы с TCP, ни на что кроме создания чата не пригодны в большинстве своем.
EasyTCP способно передавать большие объемы данных (один пакет размером в 2 гб (2130702268 байт примерно)), производить сериализацию классов и передачу их.

## [Примеры](https://github.com/Camyil-89/EasyTCP/tree/master/TestClient/Examples))

## Client
> Объяснения функционала (может быть не точным, из за внесения изменений в функции).
### Connect
> Подключение к серверу
```C#
public bool Connect(string host, int port, ISerialization serialization = null)
```
### Close
> Закрытие подключения
```C#
public void Close()
```
### EnableSsl 
> Передается сертификат и можно указать проверять или нет данный сертификат.
```C#
public void EnableSsl(X509Certificate certificate, bool IsValidateCertificate)
```
### Send
> Отправляет пакет на сервер и не ждет ответа.
```C#
public void Send(object obj)
```
### SendAndWaitResponse
> Отправляет пакет и ждет ответ от сервера.
```C#
### public T SendAndWaitResponse<T>(object obj, int timeout = int.MaxValue)
```
### SendAndReceiveInfo
> Отправляет пакет и получает ответ от сервера о том сколько байт он считал с этого пакета, и после когда сервер отправляет ответ, так же уведомляет сколько прочитал.
> 
> !!!Увеличивает накладные расходы, чтобы их уменьшить увеличьте данный параметр у сервера BlockSizeForSendInfoReceive (его можно и у клиента задать)!!!
```C#
public IEnumerable<ResponseInfo<T>> SendAndReceiveInfo<T>(T obj, int timeout = int.MaxValue)
```
