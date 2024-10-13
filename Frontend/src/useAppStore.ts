import { create } from "zustand";
import { Interval } from "./types";

type State = {
  symbol: string;
  interval: Interval;
  activeUserPanel: "AlertsButtons" | "PriceAlertForm" | "RsiAlertForm";
  pushNotificationStatus: "unloaded" | "active" | "inactive";
};

const useAppStore = create<State>(() => ({
  symbol: "",
  interval: "OneMinute",
  activeUserPanel: "AlertsButtons",
  pushNotificationStatus: "unloaded",
}));

export default useAppStore;
