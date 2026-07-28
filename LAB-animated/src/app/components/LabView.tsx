import { useState } from "react";
import { ArrowLeft, Droplets, FlaskConical, Trash2, Flame } from "lucide-react";
import { CartoonButton } from "./ui-kit";
import { CHEMICALS, REACTIONS, Chemical } from "../game/data";

interface BeakerState {
  color: string;
  contents: string[]; // chemical ids
  label: string;
  effect: null | "precipitate" | "gas" | "heat" | "colorchange";
  temp: number;
}

const EMPTY: BeakerState = { color: "transparent", contents: [], label: "Cốc rỗng", effect: null, temp: 25 };

function mixColors(a: string, b: string) {
  const p = (h: string) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
  if (!a.startsWith("#")) return b;
  const [r1, g1, b1] = p(a), [r2, g2, b2] = p(b);
  const to = (n: number) => n.toString(16).padStart(2, "0");
  return `#${to(Math.round((r1 + r2) / 2))}${to(Math.round((g1 + g2) / 2))}${to(Math.round((b1 + b2) / 2))}`;
}

export function LabView({ onExit, onIncident, onReward }: { onExit: () => void; onIncident: (note: string) => void; onReward: () => void }) {
  const [beaker, setBeaker] = useState<BeakerState>(EMPTY);
  const [amount, setAmount] = useState(30);
  const [selected, setSelected] = useState<Chemical>(CHEMICALS[0]);

  const addChemical = (chem: Chemical) => {
    setBeaker((prev) => {
      if (prev.contents.includes(chem.id)) return prev;
      const newContents = [...prev.contents, chem.id];
      const rx = REACTIONS.find((r) => r.reactants.length === newContents.length && r.reactants.every((x) => newContents.includes(x)));
      if (rx) {
        setTimeout(() => {
          if (rx.danger) onIncident(rx.note);
          else onReward();
        }, 900);
        return {
          color: rx.color,
          contents: newContents,
          label: `${rx.productName} (${rx.productFormula})`,
          effect: rx.effect,
          temp: rx.effect === "heat" ? 78 : 25,
        };
      }
      return {
        color: prev.contents.length === 0 ? chem.color : mixColors(prev.color, chem.color),
        contents: newContents,
        label: newContents.map((id) => CHEMICALS.find((c) => c.id === id)?.formula).join(" + "),
        effect: "colorchange",
        temp: prev.temp,
      };
    });
  };

  const fill = Math.min(1, beaker.contents.length * 0.28 + 0.1);

  return (
    <div className="relative size-full bg-gradient-to-b from-[#EAF4FB] to-[#D6E9F5] overflow-hidden flex flex-col">
      {/* depth: receding tiled floor */}
      <div className="absolute inset-x-0 bottom-0 h-[42%] overflow-hidden pointer-events-none">
        <div
          className="absolute left-1/2 bottom-0"
          style={{
            width: "260%", height: "260%",
            transform: "translateX(-50%) rotateX(66deg)", transformOrigin: "bottom center",
            backgroundColor: "#E7EDF3",
            backgroundImage: "linear-gradient(#C3D0DD 2px,transparent 2px),linear-gradient(90deg,#C3D0DD 2px,transparent 2px)",
            backgroundSize: "60px 60px",
          }}
        />
      </div>
      {/* depth: diagonal god rays from a side window */}
      <div className="absolute top-0 right-[20%] w-[60%] h-full pointer-events-none mix-blend-screen"
        style={{ background: "linear-gradient(205deg, rgba(255,255,255,0.5) 0%, rgba(255,255,255,0.1) 20%, transparent 42%)", transform: "skewX(-12deg)", filter: "blur(6px)" }}
      />

      {/* top bar */}
      <div className="flex items-center justify-between p-4 z-10">
        <CartoonButton onClick={onExit}><span className="flex items-center gap-1"><ArrowLeft size={16} /> Lobby</span></CartoonButton>
        <div className="cartoon-stroke rounded-full bg-white px-4 py-1.5 cartoon-shadow" style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}>
          PHÒNG THÍ NGHIỆM
        </div>
        <CartoonButton variant="blue" onClick={() => setBeaker(EMPTY)}><span className="flex items-center gap-1"><Trash2 size={16} /> Rửa</span></CartoonButton>
      </div>

      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[260px_1fr_200px] gap-4 px-4 pb-4 min-h-0">
        {/* Chemical cabinet */}
        <div className="cartoon-stroke rounded-2xl bg-white/90 cartoon-shadow p-3 flex flex-col min-h-0">
          <div className="flex items-center gap-1 mb-2" style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}><Droplets size={16} /> Tủ Hoá Chất</div>
          <div className="grid grid-cols-2 gap-2 overflow-y-auto pr-1">
            {CHEMICALS.map((c) => (
              <button
                key={c.id}
                onClick={() => setSelected(c)}
                className={`cartoon-stroke rounded-xl p-2 flex flex-col items-center gap-1 transition-transform hover:-translate-y-0.5 ${selected.id === c.id ? "bg-[var(--secondary)]" : "bg-[#F8FAFC]"}`}
              >
                <div className="size-7 rounded-full cartoon-stroke" style={{ background: c.color }} />
                <span className="text-xs" style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>{c.formula}</span>
                <span className="text-[10px] text-[var(--muted-foreground)]">{c.amount}{c.unit}</span>
              </button>
            ))}
          </div>
          {/* slider */}
          <div className="mt-3 cartoon-stroke rounded-xl p-3 bg-[#F8FAFC]">
            <div className="text-xs mb-1">Chiết <b>{selected.formula}</b>: {amount}{selected.unit}</div>
            <input type="range" min={5} max={100} value={amount} onChange={(e) => setAmount(+e.target.value)} className="w-full accent-[var(--lab-blue)]" />
            <CartoonButton variant="green" className="w-full mt-2 text-sm" onClick={() => addChemical(selected)}>Đổ vào cốc →</CartoonButton>
          </div>
        </div>

        {/* Workbench */}
        <div className="cartoon-stroke rounded-2xl bg-white/70 cartoon-shadow relative grid place-items-center min-h-[320px]">
          <div className="absolute top-3 left-1/2 -translate-x-1/2 cartoon-stroke rounded-full bg-white px-3 py-1 text-sm" style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>
            BÀN THÍ NGHIỆM
          </div>

          {/* beaker */}
          <div className="flex flex-col items-center gap-3">
            <div className="relative">
              {/* volumetric glow cast by the solution */}
              {beaker.color !== "transparent" && (
                <div className="absolute inset-0 -z-0 blur-2xl rounded-full opacity-60 pointer-events-none"
                  style={{ background: beaker.color, transform: "scale(1.3)" }} />
              )}
              {/* contact shadow on the bench */}
              <div className="absolute -bottom-1 left-1/2 -translate-x-1/2 w-28 h-4 rounded-[100%] bg-[var(--lab-stroke)]/25 blur-[3px]" />
              <svg className="relative" width={160} height={190} viewBox="0 0 160 190">
                <defs>
                  <clipPath id="beakerClip"><path d="M40 30 L40 150 Q40 165 55 165 L105 165 Q120 165 120 150 L120 30 Z" /></clipPath>
                </defs>
                <g clipPath="url(#beakerClip)">
                  <rect x="0" y={165 - fill * 130} width="160" height="190" fill={beaker.color === "transparent" ? "#E0F2FE" : beaker.color} opacity={beaker.color === "transparent" ? 0.3 : 1} />
                  {beaker.effect === "gas" && [30, 55, 80].map((cx, i) => (
                    <circle key={cx} cx={40 + cx} cy="120" r={3 + i} fill="rgba(255,255,255,0.7)">
                      <animate attributeName="cy" values="150;40" dur={`${1 + i * 0.3}s`} repeatCount="indefinite" />
                      <animate attributeName="opacity" values="0.8;0" dur={`${1 + i * 0.3}s`} repeatCount="indefinite" />
                    </circle>
                  ))}
                  {beaker.effect === "precipitate" && <rect x="40" y="150" width="80" height="15" fill="rgba(255,255,255,0.85)" />}
                </g>
                <path d="M40 30 L40 150 Q40 165 55 165 L105 165 Q120 165 120 150 L120 30" stroke="#1E293B" strokeWidth="4" fill="none" strokeLinejoin="round" />
                <line x1="122" y1="55" x2="132" y2="55" stroke="#1E293B" strokeWidth="3" />
                <line x1="122" y1="85" x2="132" y2="85" stroke="#1E293B" strokeWidth="3" />
                <line x1="122" y1="115" x2="132" y2="115" stroke="#1E293B" strokeWidth="3" />
              </svg>
              {beaker.effect === "gas" && <div className="absolute -top-2 right-0 text-2xl animate-bounce">💨</div>}
              {beaker.effect === "heat" && <Flame className="absolute -bottom-1 left-1/2 -translate-x-1/2 text-[var(--danger-red)] animate-pulse" size={28} />}
            </div>

            <div className="cartoon-stroke rounded-xl bg-white px-4 py-2 text-center" style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>
              {beaker.label}
            </div>

            {/* temperature gauge */}
            <div className="flex items-center gap-2 text-sm">
              <span>🌡️</span>
              <div className="w-32 h-3 rounded-full cartoon-stroke bg-white overflow-hidden">
                <div className="h-full transition-all" style={{ width: `${beaker.temp}%`, background: beaker.temp > 50 ? "#E74C3C" : "#00A8E8" }} />
              </div>
              <span style={{ fontWeight: 700 }}>{beaker.temp}°C</span>
            </div>
          </div>
        </div>

        {/* Tools cabinet */}
        <div className="cartoon-stroke rounded-2xl bg-white/90 cartoon-shadow p-3 flex flex-col gap-2">
          <div className="flex items-center gap-1 mb-1" style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}><FlaskConical size={16} /> Dụng Cụ</div>
          {["🧪 Ống nghiệm", "⚗️ Bình tam giác", "🥃 Cốc chia độ", "🔥 Đèn cồn", "🥢 Đũa thuỷ tinh", "⚡ Máy điện phân", "🚰 Bồn rửa"].map((t) => (
            <div key={t} className="cartoon-stroke rounded-xl p-2 bg-[#F8FAFC] text-sm text-center cursor-pointer hover:bg-[var(--secondary)] transition-colors">{t}</div>
          ))}
        </div>
      </div>
    </div>
  );
}
