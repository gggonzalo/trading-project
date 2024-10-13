import { Candle } from "@/types";
import { convertCandleEpochToLocal } from "@/utils";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

export default class CandlesStreamingService {
  private static connections: Map<string, HubConnection> = new Map();
  private static subscribers: Map<string, Set<(candle: Candle) => void>> =
    new Map();

  private static createConnection(
    symbol: string,
    interval: string,
    connectionId: string,
  ): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5215/candles-hub")
      .build();

    connection.on("CandleUpdate", ({ candle }) => {
      const localCandle = {
        ...candle,
        time: convertCandleEpochToLocal(candle.time),
      };

      this.notifySubscribers(connectionId, localCandle);
    });

    connection.start().then(() => {
      connection.invoke("SubscribeToCandleUpdates", [symbol], [interval]);
    });

    return connection;
  }

  static subscribe(
    symbol: string,
    interval: string,
    callback: (candle: Candle) => void,
  ) {
    const connectionId = symbol + interval;

    if (!this.connections.has(connectionId)) {
      const connection = this.createConnection(symbol, interval, connectionId);
      this.connections.set(connectionId, connection);

      this.subscribers.set(connectionId, new Set());
    }

    this.subscribers.get(connectionId)!.add(callback);

    return {
      unsubscribe: () => this.unsubscribe(connectionId, callback),
    };
  }

  private static unsubscribe(
    connectionId: string,
    callback: (candle: Candle) => void,
  ) {
    // Remove the callback from the subscribers
    if (this.subscribers.has(connectionId)) {
      this.subscribers.get(connectionId)!.delete(callback);
    }

    // Stop and delete the connection if there are no more subscribers
    if (this.subscribers.get(connectionId)?.size === 0) {
      this.connections.get(connectionId)?.stop();
      this.connections.delete(connectionId);
    }
  }

  private static notifySubscribers(connectionId: string, candle: Candle) {
    if (this.subscribers.has(connectionId)) {
      this.subscribers
        .get(connectionId)!
        .forEach((callback) => callback(candle));
    }
  }
}
