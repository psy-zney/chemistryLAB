import { useEffect, useState } from "react";
import { Package, ScrollText, Settings, ShoppingCart, User } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { LabDiorama } from "./LabDiorama";
import { CartoonButton, CurrencyPill } from "./ui-kit";

type Panel = "shop" | "inventory" | "quests" | "character";

const menu: { id: Panel; label: string; icon: typeof ShoppingCart }[] = [
  { id: "shop", label: "Shop", icon: ShoppingCart },
  { id: "inventory", label: "Kho", icon: Package },
  { id: "quests", label: "Nhiệm vụ", icon: ScrollText },
  { id: "character", label: "Nhân vật", icon: User },
];

export function Lobby({ dollar, diamond, exp, onOpen, onEnterLab, onEnterDesktopLab }: {
  dollar: number;
  diamond: number;
  exp: number;
  onOpen: (p: Panel) => void;
  onEnterLab: () => void;
  onEnterDesktopLab: () => void;
}) {
  const reduced = useReducedMotion();
  const [entering, setEntering] = useState(false);
  const [cleaning, setCleaning] = useState(false);
  const [toast, setToast] = useState(false);

  useEffect(() => {
    if (!cleaning) return;
    const stop = window.setTimeout(() => setCleaning(false), 1350);
    const hideToast = window.setTimeout(() => setToast(false), 2400);
    return () => { window.clearTimeout(stop); window.clearTimeout(hideToast); };
  }, [cleaning]);

  const clean = () => {
    setCleaning(true);
    setToast(true);
  };

  const enterLab = (mode: "classic" | "desktop") => {
    if (entering) return;
    setEntering(true);
    window.setTimeout(mode === "desktop" ? onEnterDesktopLab : onEnterLab, reduced ? 150 : 620);
  };

  return (
    <main className="lobby-page" aria-label="Sảnh phòng thí nghiệm">
      <LabDiorama entering={entering} cleaning={cleaning} onClean={clean} />

      <header className="lobby-hud">
        <motion.div className="lab-logo" initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: reduced ? 0.15 : 0.45, ease: [0.16, 1, 0.3, 1] }}>
          <span aria-hidden="true">🧪</span> CHEMISTRY LAB
        </motion.div>
        <div className="currency-row" aria-label="Tài nguyên hiện có">
          {[
            { icon: "$", value: dollar.toLocaleString(), tint: "var(--dollar-green)" },
            { icon: "💎", value: diamond, tint: "var(--diamond-purple)" },
            { icon: "🌟", value: exp, tint: "var(--color-lab-yellow)" },
          ].map((currency, index) => (
            <motion.div key={currency.icon} initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: reduced ? 0 : 0.12 + index * 0.08, duration: reduced ? 0.15 : 0.35, ease: [0.16, 1, 0.3, 1] }}>
              <CurrencyPill {...currency} />
            </motion.div>
          ))}
          <button type="button" aria-label="Cài đặt phòng thí nghiệm" className="settings-button"><Settings size={18} /></button>
        </div>
      </header>

      <nav className="lobby-nav" aria-label="Điều hướng sảnh">
        {menu.map((item, index) => {
          const Icon = item.icon;
          return <motion.button type="button" key={item.id} onClick={() => onOpen(item.id)} className="lobby-nav-button" initial={{ opacity: 0, x: -22 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: reduced ? 0 : 0.22 + index * 0.08, duration: reduced ? 0.15 : 0.38, ease: [0.16, 1, 0.3, 1] }}><Icon size={18} /><span>{item.label}</span></motion.button>;
        })}
      </nav>

      <div className="lobby-enter lobby-enter-actions">
        <CartoonButton variant="white" onClick={() => enterLab("classic")} className="lobby-enter-button" ariaLabel="Vào phòng thí nghiệm cổ điển">
          🔬 VÀO LAB
        </CartoonButton>
        <CartoonButton variant="blue" glow onClick={() => enterLab("desktop")} className="lobby-enter-button" ariaLabel="Mở game desktop 3D góc nhìn thứ nhất">
          DESKTOP 3D
        </CartoonButton>
      </div>

      <div className="lobby-toast" role="status" aria-live="polite" data-visible={toast}>Thiết bị đã được làm sạch.</div>
    </main>
  );
}
