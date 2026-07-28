import { lazy, Suspense, useState } from "react";
import { AnimatePresence, motion } from "motion/react";
import { LoadingScreen } from "./components/LoadingScreen";
import { Lobby } from "./components/Lobby";
import { LabView } from "./components/LabView";
import { ShopModal, InventoryModal, QuestsModal, CharacterModal, IncidentPopup } from "./components/modals";
import { CHEMICALS } from "./game/data";

type Screen = "loading" | "lobby" | "lab" | "desktop-lab";
type Panel = "shop" | "inventory" | "quests" | "character" | null;

const DesktopLabGame = lazy(() =>
  import("./components/DesktopLabGame").then((module) => ({ default: module.DesktopLabGame })),
);

export default function App() {
  const [screen, setScreen] = useState<Screen>("loading");
  const [panel, setPanel] = useState<Panel>(null);
  const [dollar, setDollar] = useState(1000);
  const [diamond, setDiamond] = useState(50);
  const [exp, setExp] = useState(120);
  const [incident, setIncident] = useState<string | null>(null);

  const buy = (price: number, currency: "dollar" | "diamond", _name: string) => {
    if (currency === "dollar") setDollar((d) => Math.max(0, d - price));
    else setDiamond((d) => Math.max(0, d - price));
  };

  return (
    <div className="size-full min-h-screen app-shell" style={{ fontFamily: "var(--font-body)" }}>
      <AnimatePresence mode="wait">
      {screen === "loading" && <motion.div key="loading" className="size-full min-h-screen" exit={{ opacity: 0 }} transition={{ duration: 0.22 }}><LoadingScreen onDone={() => setScreen("lobby")} /></motion.div>}

      {screen === "lobby" && (
        <motion.div key="lobby" className="size-full min-h-screen" initial={{ opacity: 0, scale: 0.985 }} animate={{ opacity: 1, scale: 1 }} exit={{ opacity: 0, scale: 1.012 }} transition={{ duration: 0.32, ease: [0.16, 1, 0.3, 1] }}><Lobby
          dollar={dollar}
          diamond={diamond}
          exp={exp}
          onOpen={(p) => setPanel(p)}
          onEnterLab={() => setScreen("lab")}
          onEnterDesktopLab={() => setScreen("desktop-lab")}
        /></motion.div>
      )}

      {screen === "lab" && (
        <motion.div key="lab" className="size-full min-h-screen" initial={{ opacity: 0, scale: 0.99 }} animate={{ opacity: 1, scale: 1 }} exit={{ opacity: 0, scale: 1.01 }} transition={{ duration: 0.28, ease: [0.16, 1, 0.3, 1] }}><LabView
          onExit={() => setScreen("lobby")}
          onIncident={(note) => {
            setIncident(note);
            setDollar((d) => Math.max(0, d - 150));
          }}
          onReward={() => {
            setDollar((d) => d + 60);
            setExp((e) => e + 10);
          }}
        /></motion.div>
      )}

      {screen === "desktop-lab" && (
        <motion.div key="desktop-lab" className="size-full min-h-screen" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.24, ease: [0.16, 1, 0.3, 1] }}>
          <Suspense fallback={<div className="desktop-lab-loading" role="status">Đang dựng phòng thí nghiệm 3D…</div>}>
            <DesktopLabGame
              onExit={() => setScreen("lobby")}
              onIncident={(note) => {
                setIncident(note);
                setDollar((d) => Math.max(0, d - 150));
              }}
              onReward={() => {
                setDollar((d) => d + 60);
                setExp((e) => e + 10);
              }}
            />
          </Suspense>
        </motion.div>
      )}
      </AnimatePresence>

      <AnimatePresence>
      {panel === "shop" && <ShopModal onClose={() => setPanel(null)} onBuy={buy} />}
      {panel === "inventory" && <InventoryModal onClose={() => setPanel(null)} chemicals={CHEMICALS} />}
      {panel === "quests" && (
        <QuestsModal
          onClose={() => setPanel(null)}
          onComplete={(d, e) => {
            setDollar((v) => v + d);
            setExp((v) => v + e);
          }}
        />
      )}
      {panel === "character" && <CharacterModal onClose={() => setPanel(null)} />}
      </AnimatePresence>

      <AnimatePresence>{incident && <IncidentPopup note={incident} penalty={150} onClose={() => setIncident(null)} />}</AnimatePresence>
    </div>
  );
}
