# Official HypeRate C# bindings

## Usage

1. Set your API token via the `HypeRate.GetInstance().SetApiToken(string newToken)` method
2. Call `HypeRate.GetInstance().Start()` on application startup
3. Call `HypeRate.GetInstance().Connect()` when you want to establish a connection
4. Subscribe to the events you want to receive:
   - Connected
   - Disconnected
   - ChannelJoined
   - ChannelLeft
   - HeartbeatReceived
   - ClipCreated
5. Call `HypeRate.GetInstance().JoinHeartbeatChannel()` with the user device ID for receiving updates of the users heartbeat (e.g.: "internal-testing")
6. Call `HypeRate.GetInstance().LeaveHeartbeatChannel()` with the user device ID when you not want to receive further updates of the
   users heartbeat
7. Call `HypeRate.GetInstance().Disonnect()` when you want to close the connection to the server

## Example Project

You can find an example project in the `HypeRateSample` folder. This project demonstrates how to use the library and can be run directly for a quick start.

## Working with device IDs

The `Device` class contains utility functions for working with device IDs (like validating them or extracting them from
user input).
