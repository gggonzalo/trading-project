import { NavLink, Route, Routes } from "react-router-dom";
import {
  NavigationMenu,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  navigationMenuTriggerStyle,
} from "./components/ui/navigation-menu";
import "./index.css";
import PlaceOrder from "./pages/PlaceOrder";
import RsiTracker from "./pages/RsiTracker";

function App() {
  return (
    <div className="flex flex-col items-center gap-6 py-4">
      <NavigationMenu>
        <NavigationMenuList>
          <NavigationMenuItem>
            <NavigationMenuLink
              asChild
              className={navigationMenuTriggerStyle()}
            >
              <NavLink to="/">Place order</NavLink>
            </NavigationMenuLink>
          </NavigationMenuItem>
          <NavigationMenuItem>
            <NavigationMenuLink
              asChild
              className={navigationMenuTriggerStyle()}
            >
              <NavLink to="/rsi-tracker">RSI tracker</NavLink>
            </NavigationMenuLink>
          </NavigationMenuItem>
        </NavigationMenuList>
      </NavigationMenu>
      <Routes>
        <Route path="/" element={<PlaceOrder />} />
        <Route path="/rsi-tracker" element={<RsiTracker />} />
      </Routes>
    </div>
  );
}

export default App;
