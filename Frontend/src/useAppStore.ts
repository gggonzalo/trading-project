import { create } from "zustand";
import { Interval, SymbolInfo } from "./types";

type State = {
  symbolInfo: SymbolInfo | null;
  symbolInfoStatus: "unloaded" | "loading" | "loaded";
  interval: Interval;
  activeUserPanel: "AlertsButtons" | "PriceAlertForm" | "RsiAlertForm";
  pushNotificationStatus: "unloaded" | "active" | "inactive";
};

const useAppStore = create<State>(() => ({
  symbolInfo: null,
  symbolInfoStatus: "unloaded",
  interval: "OneMinute",
  activeUserPanel: "AlertsButtons",
  pushNotificationStatus: "unloaded",
}));

export default useAppStore;
