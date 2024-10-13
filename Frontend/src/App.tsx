import "./index.css";
import Dashboard from "./pages/Dashboard";

import { useEffect } from "react";
import OneSignal from "react-onesignal";
import { TooltipProvider } from "./components/ui/tooltip";
import useAppStore from "./useAppStore";

function App() {
  // Browser web push requirements reference => https://documentation.onesignal.com/docs/web-push-setup-faq
  useEffect(() => {
    OneSignal.init({
      appId: "207a026a-1076-44d4-bb6c-f2a5804b122f",
      allowLocalhostAsSecureOrigin: true,
    }).then(() => {
      useAppStore.setState({
        pushNotificationStatus: OneSignal.Notifications.permission
          ? "active"
          : "inactive",
      });

      OneSignal.Notifications.addEventListener(
        "permissionChange",
        (newPermission) => {
          useAppStore.setState({
            pushNotificationStatus: newPermission ? "active" : "inactive",
          });
        },
      );
    });
  }, []);

  return (
    <TooltipProvider>
      <div className="py-4">
        <Dashboard />
      </div>
    </TooltipProvider>
  );
}

export default App;
