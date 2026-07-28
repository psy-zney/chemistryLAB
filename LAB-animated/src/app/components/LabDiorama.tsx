import { CSSProperties, MouseEvent, ReactNode, useRef } from "react";
import { motion, useMotionValue, useReducedMotion, useSpring } from "motion/react";
import { Chibi } from "./Chibi";

export type ChemicalEffect = "none" | "bubbles" | "gas" | "precipitate" | "heat";

export interface LiquidProps {
  color: string;
  fillLevel: number;
  glow?: boolean;
  effect?: ChemicalEffect;
  active?: boolean;
}

type DioramaProps = {
  entering?: boolean;
  cleaning?: boolean;
  onClean: () => void;
};

export function LabDiorama({ entering = false, cleaning = false, onClean }: DioramaProps) {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotion();
  const rawX = useMotionValue(0);
  const rawY = useMotionValue(0);
  const nearX = useSpring(rawX, { stiffness: 80, damping: 22 });
  const nearY = useSpring(rawY, { stiffness: 80, damping: 22 });
  const farX = useSpring(rawX, { stiffness: 48, damping: 24 });
  const farY = useSpring(rawY, { stiffness: 48, damping: 24 });

  const move = (event: MouseEvent<HTMLDivElement>) => {
    if (reduced || !ref.current) return;
    const box = ref.current.getBoundingClientRect();
    rawX.set(Math.max(-6, Math.min(6, ((event.clientX - box.left) / box.width - 0.5) * 12)));
    rawY.set(Math.max(-5, Math.min(5, ((event.clientY - box.top) / box.height - 0.5) * 10)));
  };

  return (
    <div ref={ref} className="lab-diorama" onMouseMove={move} onMouseLeave={() => { rawX.set(0); rawY.set(0); }}>
      <motion.div className="diorama-backwall" style={{ x: farX, y: farY }} />
      <motion.div className="diorama-right-light" style={{ x: farX, y: farY }} />
      <PerspectiveFloor />
      <motion.div className="diorama-motion-layer" style={{ x: farX, y: farY }}>
        <div className="diorama-sink-wrap"><SinkStation cleaning={cleaning} onClean={onClean} /></div>
      </motion.div>
      <motion.div className="diorama-motion-layer" style={{ x: nearX, y: nearY }}>
        <div className="cabinet-wrap cabinet-wrap--left"><FrostedCabinet side="left" /></div>
      </motion.div>
      <motion.div className="diorama-motion-layer" style={{ x: nearX, y: nearY }}>
        <div className="cabinet-wrap cabinet-wrap--right"><FrostedCabinet side="right" /></div>
      </motion.div>
      <motion.div className="diorama-motion-layer" style={{ x: nearX, y: nearY }}>
        <div className={`diorama-scientist ${entering ? "diorama-scientist--entering" : ""}`}><Chibi hair="#D97706" size={330} animated wave={entering} /></div>
      </motion.div>
      <motion.div className="diorama-motion-layer" style={{ x: nearX, y: nearY }}>
        <div className="diorama-workbench"><MainWorkbench /></div>
      </motion.div>
    </div>
  );
}

function PerspectiveFloor() {
  const floorLines = [0, 85, 170, 255, 340, 425, 510, 595, 680, 765, 850, 935, 1020];
  const crossLines = [36, 90, 160, 252, 370, 520];
  return (
    <div className="diorama-floor" aria-hidden="true">
      <svg viewBox="0 0 1020 520" preserveAspectRatio="none" className="diorama-floor-grid">
        <defs><clipPath id="diorama-floor-clip"><path d="M130 0 H890 L1020 520 H0 Z" /></clipPath></defs>
        <g clipPath="url(#diorama-floor-clip)" stroke="var(--color-lab-slate-soft)" strokeWidth="2" opacity="0.48">
          {floorLines.map((x) => <line key={x} x1="510" y1="0" x2={x} y2="520" />)}
          {crossLines.map((y) => <line key={y} x1="0" y1={y} x2="1020" y2={y} />)}
        </g>
      </svg>
    </div>
  );
}

function SinkStation({ cleaning, onClean }: { cleaning: boolean; onClean: () => void }) {
  return (
    <button type="button" className="sink-station" onClick={onClean} aria-label="Rửa sạch thiết bị tại bồn rửa">
      <span className="sink-faucet" />
      <span className="sink-faucet-neck" />
      <span className="sink-rack" aria-hidden="true">{[0, 1, 2, 3, 4].map((item) => <i key={item} />)}</span>
      <span className="sink-body"><span className="sink-basin" /></span>
      {cleaning && <span className="sink-water" aria-hidden="true" />}
    </button>
  );
}

function FrostedCabinet({ side }: { side: "left" | "right" }) {
  const bottleColors = side === "left"
    ? ["#A855F7", "#2ECC71", "#FDE68A", "#60A5FA", "#C084FC", "#22D3EE"]
    : ["#C084FC", "#FDE68A", "#60A5FA", "#2ECC71", "#FB7185", "#FACC15"];
  return (
    <div className={`frosted-cabinet frosted-cabinet--${side}`} aria-label={side === "left" ? "Tủ kính hóa chất trái" : "Tủ kính hóa chất phải"}>
      <div className="cabinet-ambient" />
      <div className="cabinet-frame">
        <div className="cabinet-door">
          {[0, 1, 2].map((shelf) => (
            <div className="cabinet-shelf" key={shelf}>
              {bottleColors.slice(shelf * 2, shelf * 2 + 2).map((color, index) => <CabinetBottle key={color} color={color} tall={index === 1} />)}
            </div>
          ))}
          <span className="cabinet-shimmer" />
        </div>
        <span className="cabinet-handle" />
      </div>
      <span className="cabinet-feet cabinet-feet--a" /><span className="cabinet-feet cabinet-feet--b" />
    </div>
  );
}

function CabinetBottle({ color, tall }: { color: string; tall: boolean }) {
  return <span className={`cabinet-bottle ${tall ? "cabinet-bottle--tall" : ""}`} style={{ "--bottle-color": color } as CSSProperties}><i /></span>;
}

function MainWorkbench() {
  return (
    <div className="main-workbench">
      <div className="bench-contact-shadow" />
      <div className="bench-top">
        <div className="bench-items">
          <ErlenmeyerFlask color="#A855F7" fillLevel={0.62} glow effect="bubbles" active />
          <TestTubeRack />
          <RoundFlask color="#22D3EE" fillLevel={0.58} glow effect="bubbles" active />
        </div>
      </div>
      <div className="bench-front"><div className="bench-inset" /></div>
      <div className="bench-leg bench-leg--left" /><div className="bench-leg bench-leg--right" />
    </div>
  );
}

function GlassLift({ children, className = "" }: { children: ReactNode; className?: string }) {
  const reduced = useReducedMotion();
  return <motion.div className={`glass-lift ${className}`} whileHover={reduced ? undefined : { y: -6 }} transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}>{children}</motion.div>;
}

export function ErlenmeyerFlask({ color, fillLevel, glow = false, effect = "none", active = false }: LiquidProps) {
  const liquidY = 134 - fillLevel * 58;
  return (
    <GlassLift className="erlenmeyer-flask">
      <div className={`glass-glow ${glow ? "glass-glow--active" : ""}`} style={{ background: color }} />
      <div className="glass-shadow" />
      <svg viewBox="0 0 110 146" role="img" aria-label="Bình tam giác chứa dung dịch tím">
        <defs><clipPath id="erlenmeyer-clip"><path d="M43 24 L43 62 L16 126 Q12 138 27 140 H83 Q98 138 94 126 L67 62 L67 24 Z" /></clipPath></defs>
        <g clipPath="url(#erlenmeyer-clip)">
          <rect x="0" y={liquidY} width="110" height="150" fill={color} opacity="0.82" />
          <path d={`M18 ${liquidY + 2} Q55 ${liquidY - 3} 92 ${liquidY + 2}`} stroke="rgba(255,255,255,.85)" strokeWidth="2.5" fill="none" />
          <ChemicalParticles effect={effect} active={active} />
        </g>
        <path d="M43 24 L43 62 L16 126 Q12 138 27 140 H83 Q98 138 94 126 L67 62 L67 24" fill="var(--color-lab-glass)" stroke="var(--color-lab-slate)" strokeWidth="4" strokeLinejoin="round" />
        <rect x="39" y="15" width="32" height="12" rx="5" fill="rgba(227,247,255,.8)" stroke="var(--color-lab-slate)" strokeWidth="3" />
        <path d="M29 101 L37 78" stroke="rgba(255,255,255,.75)" strokeWidth="4" strokeLinecap="round" />
      </svg>
    </GlassLift>
  );
}

export function RoundFlask({ color, fillLevel, glow = false, effect = "none", active = false }: LiquidProps) {
  const liquidY = 120 - fillLevel * 49;
  return (
    <GlassLift className="round-flask">
      <div className={`glass-glow ${glow ? "glass-glow--active" : ""}`} style={{ background: color }} />
      <div className="glass-shadow" />
      <svg viewBox="0 0 130 150" role="img" aria-label="Bình cầu chứa dung dịch cyan">
        <defs><clipPath id="round-clip"><path d="M51 22 H79 V69 A46 46 0 1 1 51 69 Z" /></clipPath></defs>
        <g clipPath="url(#round-clip)">
          <rect x="0" y={liquidY} width="130" height="150" fill={color} opacity="0.82" />
          <path d={`M20 ${liquidY + 2} Q65 ${liquidY - 5} 110 ${liquidY + 2}`} stroke="rgba(255,255,255,.9)" strokeWidth="3" fill="none" />
          <ChemicalParticles effect={effect} active={active} />
        </g>
        <path d="M51 22 H79 V69 A46 46 0 1 1 51 69 Z" fill="var(--color-lab-glass)" stroke="var(--color-lab-slate)" strokeWidth="4" strokeLinejoin="round" />
        <rect x="48" y="14" width="34" height="12" rx="5" fill="rgba(227,247,255,.8)" stroke="var(--color-lab-slate)" strokeWidth="3" />
        <path d="M38 91 Q31 105 37 115" stroke="rgba(255,255,255,.82)" strokeWidth="6" strokeLinecap="round" fill="none" />
      </svg>
    </GlassLift>
  );
}

export function TestTubeRack() {
  return (
    <GlassLift className="test-tube-rack">
      <div className="glass-shadow" />
      <svg viewBox="0 0 150 135" role="img" aria-label="Giá ba ống nghiệm đỏ, xanh lá và vàng">
        {[{ x: 30, color: "#EF4444", level: 0.67 }, { x: 67, color: "#2ECC71", level: 0.48 }, { x: 104, color: "#FACC15", level: 0.75 }].map((tube, index) => {
          const y = 108 - tube.level * 63;
          return <g key={tube.color}>
            <path d={`M${tube.x} 15 V98 Q${tube.x} 108 ${tube.x + 9} 108 Q${tube.x + 18} 108 ${tube.x + 18} 98 V15 Z`} fill="var(--color-lab-glass)" stroke="var(--color-lab-slate)" strokeWidth="3" />
            <path d={`M${tube.x + 2} ${y} H${tube.x + 16} V98 Q${tube.x + 9} 105 ${tube.x + 2} 98 Z`} fill={tube.color} opacity="0.85" />
            <path d={`M${tube.x + 3} ${y + 2} Q${tube.x + 9} ${y - 1} ${tube.x + 15} ${y + 2}`} stroke="rgba(255,255,255,.85)" strokeWidth="1.8" fill="none" />
            {index !== 1 && <circle cx={tube.x + 11} cy={y + 16} r="2" fill="rgba(255,255,255,.75)"><animate attributeName="cy" values={`${y + 32};${y + 10}`} dur={`${1.3 + index * 0.3}s`} repeatCount="indefinite" /></circle>}
          </g>;
        })}
        <path d="M16 84 H134 V110 H16 Z" fill="#F5B51B" stroke="var(--color-lab-slate)" strokeWidth="4" strokeLinejoin="round" />
        <path d="M24 92 H126" stroke="#FFF4C6" strokeWidth="3" strokeLinecap="round" />
      </svg>
    </GlassLift>
  );
}

function ChemicalParticles({ effect, active }: Pick<LiquidProps, "effect" | "active">) {
  if (!active || effect === "none") return null;
  if (effect === "precipitate") return <path d="M18 128 Q55 118 92 128 V142 H18Z" fill="rgba(255,255,255,.85)" />;
  if (effect === "heat") return <path d="M45 118 C34 103 49 92 45 80 C60 92 68 105 58 120Z" fill="rgba(250,204,21,.78)" />;
  return <>{[0, 1, 2].map((particle) => <circle key={particle} cx={34 + particle * 18} cy="122" r={2.4 + particle * 0.6} fill="rgba(255,255,255,.78)"><animate attributeName="cy" values={`130;${68 + particle * 8}`} dur={`${1.5 + particle * 0.25}s`} begin={`${particle * 0.2}s`} repeatCount="indefinite" /><animate attributeName="opacity" values=".8;0" dur={`${1.5 + particle * 0.25}s`} begin={`${particle * 0.2}s`} repeatCount="indefinite" /></circle>)}</>;
}
