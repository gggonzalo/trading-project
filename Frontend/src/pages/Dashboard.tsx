import SymbolCandleStickChart from "@/components/charts/SymbolCandleStickChart";
import SymbolsCombobox from "@/components/SymbolsCombobox";
import { Button } from "@/components/ui/button";
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
} from "@/components/ui/resizable";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import OneSignal from "react-onesignal";

import PriceAlertForm from "@/components/PriceAlertForm";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableNoDataRow,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Alert, Interval } from "@/types";
import useAppStore from "@/useAppStore";
import { useViewportSize } from "@mantine/hooks";
import {
  BellRing,
  CheckCircle2,
  Plus,
  TrendingDown,
  TrendingUp,
  XCircle,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";

function Dashboard() {
  // Store
  const interval = useAppStore((state) => state.interval);
  const activeUserPanel = useAppStore((state) => state.activeUserPanel);
  const pushNotificationsStatus = useAppStore(
    (state) => state.pushNotificationStatus,
  );

  // State
  const [selectedSymbol, setSelectedSymbol] = useState<string>("");
  const [symbolChangeAbortController, setSymbolChangeAbortController] =
    useState<AbortController | null>(null);
  const [alerts, setAlerts] = useState<Alert[]>([]);

  const { width } = useViewportSize();

  const handleSymbolChange = async (newSymbol: string) => {
    // Abort the previous request if it exists
    if (symbolChangeAbortController) {
      symbolChangeAbortController.abort();
    }

    const newAbortController = new AbortController();
    setSymbolChangeAbortController(newAbortController);

    setSelectedSymbol(newSymbol);
    useAppStore.setState({ symbolInfo: null, symbolInfoStatus: "loading" });

    if (!newSymbol) {
      useAppStore.setState({ symbolInfoStatus: "unloaded" });

      return;
    }

    try {
      const newSymbolInfoResponse = await fetch(
        `http://localhost:5215/symbols/${newSymbol}`,
        {
          signal: newAbortController.signal,
        },
      );

      if (!newSymbolInfoResponse.ok) throw new Error("Symbol info not found");

      const newSymbolInfo = await newSymbolInfoResponse.json();

      useAppStore.setState({
        symbolInfo: newSymbolInfo,
        symbolInfoStatus: "loaded",
      });
    } catch (e) {
      if (e instanceof Error) {
        if (e.name === "AbortError") return;

        setSelectedSymbol("");
        useAppStore.setState({ symbolInfoStatus: "unloaded" });
      }
    }
  };

  const fetchUserAlerts = useCallback(() => {
    if (pushNotificationsStatus === "unloaded") return;

    fetch(
      `http://localhost:5215/alerts?subscriptionId=${OneSignal.User.PushSubscription.id}`,
    )
      .then((response) => response.json())
      .then((data) => {
        setAlerts(data);
      });
  }, [pushNotificationsStatus]);

  useEffect(() => {
    fetchUserAlerts();
  }, [fetchUserAlerts]);

  const renderSymbolIntervalControls = () => {
    return (
      <div className="flex gap-1">
        <SymbolsCombobox
          value={selectedSymbol}
          onValueChange={handleSymbolChange}
        />
        <Select
          value={interval}
          onValueChange={(value) =>
            useAppStore.setState({ interval: value as Interval })
          }
        >
          <SelectTrigger className="w-[85px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem key="OneMinute" value="OneMinute">
              1m
            </SelectItem>
            <SelectItem key="FiveMinutes" value="FiveMinutes">
              5m
            </SelectItem>
            <SelectItem key="FifteenMinutes" value="FifteenMinutes">
              15m
            </SelectItem>
            <SelectItem key="OneHour" value="OneHour">
              1h
            </SelectItem>
            <SelectItem key="FourHour" value="FourHour">
              4h
            </SelectItem>
            <SelectItem key="OneDay" value="OneDay">
              1d
            </SelectItem>
            <SelectItem key="OneWeek" value="OneWeek">
              1w
            </SelectItem>
            <SelectItem key="OneMonth" value="OneMonth">
              1M
            </SelectItem>
          </SelectContent>
        </Select>
        {width < 768 && (
          <Popover>
            <PopoverTrigger asChild>
              <Button className="ml-auto">
                <BellRing className="size-4" />
              </Button>
            </PopoverTrigger>
            <PopoverContent
              collisionPadding={{
                right: 16,
              }}
            >
              {renderUserPanel()}
            </PopoverContent>
          </Popover>
        )}
      </div>
    );
  };

  const renderUserPanel = () => {
    if (activeUserPanel === "AlertsButtons") {
      const renderPushNotificationsInfo = () => {
        if (pushNotificationsStatus === "active") {
          return (
            <p className="flex items-center text-green-600">
              <CheckCircle2 className="mr-1" size={20} />
              <span>Push notifications are enabled</span>
            </p>
          );
        }

        if (pushNotificationsStatus === "inactive") {
          return (
            <div className="flex flex-col items-center gap-1">
              <p className="flex items-center text-destructive">
                <XCircle className="mr-1" size={20} />
                <span>Push notifications are disabled</span>
              </p>
              <p
                className="cursor-pointer text-sm text-blue-600"
                onClick={() => OneSignal.Slidedown.promptPush()}
              >
                Click here to enable
              </p>
            </div>
          );
        }
      };

      return (
        <div className="flex flex-col items-center justify-center">
          <div className="mb-6 flex flex-col gap-2">
            <Button
              type="button"
              size="lg"
              onClick={() =>
                useAppStore.setState({ activeUserPanel: "PriceAlertForm" })
              }
            >
              <Plus className="mr-1" size={24} />
              Create price alert
            </Button>
            <Tooltip>
              <TooltipTrigger asChild>
                <span tabIndex={-1}>
                  <Button type="button" className="w-full" size="lg" disabled>
                    <Plus className="mr-1" size={24} />
                    Create RSI alert
                  </Button>
                </span>
              </TooltipTrigger>
              <TooltipContent side="bottom">
                <p>Coming soon...</p>
              </TooltipContent>
            </Tooltip>
          </div>
          {renderPushNotificationsInfo()}
        </div>
      );
    }

    if (activeUserPanel === "PriceAlertForm") {
      return <PriceAlertForm onAlertCreated={fetchUserAlerts} />;
    }
  };

  const renderTradingContent = () => {
    if (width > 768) {
      return (
        <div className="h-[24rem]">
          <ResizablePanelGroup direction="horizontal">
            <ResizablePanel
              className="flex flex-col gap-2 p-3"
              defaultSize={60}
              minSize={50}
              maxSize={70}
            >
              {renderSymbolIntervalControls()}
              <SymbolCandleStickChart />
            </ResizablePanel>
            <ResizableHandle withHandle />
            <ResizablePanel>
              <div className="flex size-full items-center justify-center p-3">
                {renderUserPanel()}
              </div>
            </ResizablePanel>
          </ResizablePanelGroup>
        </div>
      );
    }

    return (
      <div className="flex h-[28rem] flex-col gap-2">
        {renderSymbolIntervalControls()}
        <SymbolCandleStickChart />
      </div>
    );
  };

  return (
    <div className="container mx-auto">
      <div className="flex flex-col rounded md:border">
        {renderTradingContent()}
        <Tabs defaultValue="alerts">
          <TabsList className="w-full">
            <TabsTrigger value="alerts">Alerts</TabsTrigger>
          </TabsList>
          <TabsContent value="alerts">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Symbol</TableHead>
                  <TableHead>Value On Creation</TableHead>
                  <TableHead>Value Target</TableHead>
                  <TableHead>Creation Time</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {alerts.length === 0 ? (
                  <TableNoDataRow>No alerts.</TableNoDataRow>
                ) : (
                  alerts.map((alert) => {
                    const isAlertBullish =
                      alert.valueTarget > alert.valueOnCreation;

                    return (
                      <TableRow key={alert.id}>
                        <TableCell>{alert.symbol}</TableCell>
                        <TableCell>{alert.valueOnCreation}</TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            {isAlertBullish ? (
                              <TrendingUp className="size-5 stroke-[#26a69a]" />
                            ) : (
                              <TrendingDown className="size-5 stroke-[#ef5350]" />
                            )}
                            {alert.valueTarget}
                          </div>
                        </TableCell>
                        <TableCell>
                          {new Intl.DateTimeFormat("en-US", {
                            dateStyle: "short",
                            timeStyle: "medium",
                          }).format(new Date(alert.createdAt))}
                        </TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}

export default Dashboard;
