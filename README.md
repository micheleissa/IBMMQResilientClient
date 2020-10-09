# IBMMQResilientClient
dotnet core client for IBM MQ with built-in resiliency and the ability to connect securely.   

# Features:
 - Retry capability via Polly.
 - TLS support(optional).
## Retry:
 - Polly is used for resiliency and the retry option allows for exponential backoff.
 ```
 // Retry a specified number of times, using a function to 
// calculate the duration to wait between retries based on 
// the current retry attempt (allows for exponential backoff)
// In this case will wait for
//  2 ^ 1 = 2 seconds then
//  2 ^ 2 = 4 seconds then
//  2 ^ 3 = 8 seconds then
//  2 ^ 4 = 16 seconds then
//  2 ^ 5 = 32 seconds
Policy
  .Handle<SomeExceptionType>()
  .WaitAndRetry(5, retryAttempt => 
	TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) 
  );
  ````
  - Again it is not configurable but it is a possibility in the future.
## TLS support:
- As of now the support is not fully configurable, it supports one CipherSpec ```TLS_RSA_WITH_AES_128_CBC_SHA256```.
- the library is smart about the installation of the certs and will figure out which OS and installs accordingly. 

# HowTo:
 - Add configuration/settings:
 ```
 "queueOptions": {
		"managerName": "QM1",
		"name": "DEV.QUEUE.1",
		"appName": "MyApp",
		"channel": "DEV.APP.SVRCONN",
		"userName": "app",
		"password": "passw0rd",
		"installCert": false,//when set to true certs must be provided.
		"clientCert": "",
		"serverCert": "",
		"subscriptionName": "subName",
		"topic": "dev/",
		"mqHostOptionsList": [
			{
				"hostName": "localhost",
				"port": "1414"
			}
		]
}
 ```
 - Register the needed services with DI container by calling the extension method:
 ```services..AddIbmMQ();```
 - Now you should be ready to write/read from the queue.

 A sample console app which connects to local IBM MQ container instance is include for references.