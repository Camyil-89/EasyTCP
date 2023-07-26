![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/424aff49-3db3-4c86-b003-9d288a105bfc)
# EasyTCP
Библиотека для создания простого клиент-серверного приложения. Эта библиотека решает такую проблему как отправка больших пакетов, путем использования своего протокола передачи данных (работает поверх TCP). Большинство библиотек которые есть в интернете для работы с TCP, ни на что кроме создания чата не пригодны в большинстве своем.
EasyTCP способно передавать большие объемы данных (один пакет размером в 2 гб (2130702268 байт примерно)), производить сериализацию классов и передачу их.

## [Примеры](https://github.com/Camyil-89/EasyTCP/tree/master/TestClient/Examples)

## Протокол EasyTCP
Протокол EasyTCP работает поверх TCP что обеспечивает целостность доставки данных.
> Структура заголовка пакета EasyTCP. Общая длина заголовка 13 байт.
<table>
  <tr>
    <tr>
            <td colspan="8" align="center">1 - 8 Байт</td>
        </tr>
        <tr>
            <td colspan="1" align="center">Version (1 Байт)</td>
			<td colspan="4" align="center">DataSize (4 Байта)</td>
			<td colspan="3" align="center">UID (4 Байта)</td>
        </tr>
		 <tr>
            <td colspan="1">UID (перенос)</td>
			<td colspan="1" align="center">Mode (1 Байт)</td>
			<td colspan="1" align="center">Type (1 Байт)</td>
			<td colspan="2" align="center">TypePacket (2 Байт)</td>
			<td colspan="1" align="center">Данные</td>
			<td colspan="1" align="center">Данные</td>
			<td colspan="1" align="center">Данные</td>
        </tr>
		<tr>
            <td colspan="8" align="center">Данные</td>
        </tr>
</table>

>Описание параметров заголовка EasyTCP.

|Параметры|Размер (Байт) | Описание|
|-|-|-|
|Version|1|Версия протокола EasyTCP|
|DataSize|4|Длина данных после заголовка|
|UID|4|Уникальный идентификатор для обработки ожидаемых пакетов.|
|Mode|1|Определяет режим передачи пакета.<br>Hidden (0) - не уведомляет клиента о количестве считанных байт из отправленного клиентом пакета.<br>Info (1) - уведомляет клиента о количестве считанных байт из отправленного клиентом пакета|
|Type|1|Тип пакета который передается. Все служебные пакеты начинаются с 1 по 6, 0 - передача клиентского пакета|
|TypePacket|2|Указывает что это за пакет для PacketEntityManager, 0 - зарезервирован для не определенного пакета.|
|Общая длина пакета|13|

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
public T SendAndWaitResponse<T>(object obj, int timeout = int.MaxValue)
```
### SendAndReceiveInfo
> Отправляет пакет и получает ответ от сервера о том сколько байт он считал с этого пакета, и после когда сервер отправляет ответ, так же уведомляет сколько прочитал.
> 
> !!!Увеличивает накладные расходы, чтобы их уменьшить увеличьте данный параметр у сервера BlockSizeForSendInfoReceive (его можно и у клиента задать)!!!
```C#
public IEnumerable<ResponseInfo<T>> SendAndReceiveInfo<T>(T obj, int timeout = int.MaxValue)
```
