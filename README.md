# IBMMQResilientClient   ![Nuget](https://img.shields.io/nuget/dt/IBMMQResilientClient?style=flat-square)
dotnet core client for IBM MQ with built-in resiliency and the ability to connect securely.   

# Features:
 - Retry capability via Polly.
 - TLS support(optional).
## Retry:
 - [Polly](https://github.com/App-vNext/Polly) is used for resiliency and the retry option allows for exponential backoff.
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
  - configurable retry count.
## TLS support:
- Configurable Cipher Spec.
- The library is smart about the installation of the certs and will figure out which OS and installs accordingly.
- The library supports MQManager failover via allowing multiple host(port) pairs in the configuration settings. 

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
		"cipherSpec": "VALID_CIPHER_SPEC", //Optional if not set will defaulted to TLS_RSA_WITH_AES_128_CBC_SHA256
		"retryCount": INT_VALUE, //Optional if not set will be defaulted to 5 used to control exponential backoff of Polly retry logic
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
