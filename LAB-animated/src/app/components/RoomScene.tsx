import { ReactNode } from "react";

/** A one-point-perspective room shell. All furniture below is front elevation. */
export function RoomScene({ children, tint = "#E0F2FE" }: { children?: ReactNode; tint?: string }) {
  return (
    <div className="absolute inset-0 overflow-hidden select-none">
      {/* Back wall — the only vertical plane behind the props */}
      <div className="absolute inset-x-[11%] top-0 h-[52%] bg-[#c8e5f6]" />
      <div
        className="absolute inset-x-[11%] top-0 h-[52%] opacity-70"
        style={{ backgroundImage: "linear-gradient(rgba(86,126,159,.20) 1px, transparent 1px), linear-gradient(90deg, rgba(86,126,159,.20) 1px, transparent 1px)", backgroundSize: "48px 48px" }}
      />

      {/* Perspective side walls stop at the floor seam */}
      <div className="absolute inset-y-0 left-0 w-[14%] bg-[#9bc3dd] [clip-path:polygon(0_0,100%_0,100%_52%,0_72%)]" />
      <div className="absolute inset-y-0 right-0 w-[14%] bg-[#9bc3dd] [clip-path:polygon(0_0,100%_0,100%_72%,0_52%)]" />
      <div className="absolute left-[11%] top-0 h-[52%] w-[3px] bg-[#496e87]/35" />
      <div className="absolute right-[11%] top-0 h-[52%] w-[3px] bg-[#496e87]/35" />

      {/* Strong, unambiguous wall / floor separation */}
      <div className="absolute inset-x-0 top-[51.3%] z-[2] h-4 bg-gradient-to-b from-[#7692a5] via-[#a9c1d0] to-[#d5e1e9] shadow-[0_5px_8px_rgba(30,41,59,.24)]" />

      {/* Floor is a trapezoid and its grid converges to the wall seam. */}
      <div className="absolute inset-x-0 bottom-0 top-[52%] bg-[#b9cfdd]" />
      <svg className="absolute inset-x-0 bottom-0 h-[48%] w-full" viewBox="0 0 1000 480" preserveAspectRatio="none" aria-hidden="true">
        <defs>
          <clipPath id="floor-trapezoid"><path d="M110 0 H890 L1000 480 H0 Z" /></clipPath>
        </defs>
        <g clipPath="url(#floor-trapezoid)" stroke="#7795a8" strokeWidth="1.4" opacity="0.62">
          {[0, 85, 170, 255, 340, 425, 500, 575, 660, 745, 830, 915, 1000].map((x) => <line key={x} x1="500" y1="0" x2={x} y2="480" />)}
          {[40, 100, 175, 270, 380, 480].map((y) => <line key={y} x1="0" y1={y} x2="1000" y2={y} />)}
        </g>
      </svg>
      <div className="absolute inset-x-0 top-[52%] z-[3] h-7 bg-gradient-to-b from-black/15 to-transparent" />

      {/* A compact rear sink stays on the wall and never crosses the character. */}
      <div className="absolute left-1/2 top-[8%] z-[4] -translate-x-1/2 scale-[.82] origin-top"><RearSink /></div>

      {/* Freestanding cabinets: vertical, front-facing, with feet planted on the floor. */}
      <div className="absolute bottom-[13%] left-[15%] z-[8] hidden min-[900px]:block"><StandingCabinet kind="chemicals" /></div>
      <div className="absolute bottom-[13%] right-[15%] z-[8] hidden min-[900px]:block"><StandingCabinet kind="tools" /></div>

      <div className="absolute inset-0 pointer-events-none" style={{ background: `radial-gradient(90% 75% at 50% 45%, transparent 45%, ${tint}55 100%)` }} />
      <div className="absolute inset-0 z-[10]">{children}</div>
    </div>
  );
}

export function ContactShadow({ className = "", w = 180 }: { className?: string; w?: number }) {
  return <div className={`h-5 rounded-[100%] bg-[#1e293b]/25 blur-md ${className}`} style={{ width: w }} />;
}

function RearSink() {
  return (
    <div className="relative w-[250px] pt-16">
      <div className="absolute left-1/2 top-0 h-16 w-12 -translate-x-1/2 rounded-t-[28px] border-[4px] border-[#1e293b] border-b-0 bg-[#a9d8ec]" />
      <div className="absolute left-[57%] top-[44px] h-4 w-10 rounded-r-full border-b-[4px] border-[#1e293b] bg-[#a9d8ec]" />
      <div className="absolute left-[28px] top-[50px] h-8 w-3 rounded bg-[#6e91a8]" />
      <div className="absolute right-[28px] top-[50px] h-8 w-3 rounded bg-[#6e91a8]" />
      <div className="h-4 rounded-t-md border-[3px] border-[#1e293b] bg-[#f8fbff]" />
      <div className="relative h-20 rounded-b-xl border-x-[3px] border-b-[3px] border-[#1e293b] bg-[#dbe8f1]">
        <div className="absolute left-1/2 top-3 h-12 w-36 -translate-x-1/2 rounded-xl border-[3px] border-[#1e293b] bg-[#42647d] shadow-inner" />
      </div>
    </div>
  );
}

function StandingCabinet({ kind }: { kind: "chemicals" | "tools" }) {
  const chemicalColors = ["#fb7185", "#fbbf24", "#35cf83", "#a855f7", "#38bdf8", "#fb923c"];
  const isChemicals = kind === "chemicals";
  return (
    <div className="relative w-[150px]">
      <div className="absolute -bottom-5 left-3 right-3 h-6 rounded-[100%] bg-[#1e293b]/20 blur-md" />
      <div className="relative rounded-xl border-[3px] border-[#1e293b] bg-[#e7f0f6] p-2 shadow-[5px_7px_0_rgba(30,41,59,.16)]">
        <div className={`mb-2 rounded-lg border-[3px] border-[#1e293b] px-2 py-1 text-center text-[11px] font-extrabold tracking-wide ${isChemicals ? "bg-[#fff1b6] text-[#914d12]" : "bg-[#dff4ff] text-[#27617d]"}`}>
          {isChemicals ? "HÓA CHẤT" : "DỤNG CỤ"}
        </div>
        <div className="rounded-md border-[3px] border-[#1e293b] bg-[#9fc5dd] p-1.5">
          {[0, 1, 2].map((row) => (
            <div key={row} className="flex h-14 items-end justify-around border-b-2 border-[#496e87]/45 last:border-0">
              {isChemicals ? chemicalColors.slice(row * 2, row * 2 + 2).map((color, index) => (
                <Bottle key={color} color={color} tall={index === 1 && row !== 1} />
              )) : <GlassTools key={row} row={row} />}
            </div>
          ))}
        </div>
      </div>
      <div className="absolute -bottom-8 left-4 h-8 w-4 rounded-b bg-[#526f82] border-x-[3px] border-b-[3px] border-[#1e293b]" />
      <div className="absolute -bottom-8 right-4 h-8 w-4 rounded-b bg-[#526f82] border-x-[3px] border-b-[3px] border-[#1e293b]" />
    </div>
  );
}

function Bottle({ color, tall }: { color: string; tall?: boolean }) {
  return <div className={`relative w-7 rounded-b-md border-[2.5px] border-[#1e293b] ${tall ? "h-10" : "h-8"}`} style={{ background: color }}><div className="absolute -top-2 left-1/2 h-3 w-3 -translate-x-1/2 rounded-t border-[2.5px] border-[#1e293b] bg-[#d8edf9]" /><i className="absolute left-1 top-1 h-4 w-1 rounded bg-white/60" /></div>;
}

function GlassTools({ row }: { row: number }) {
  if (row === 0) return <><div className="h-10 w-7 rounded-b-lg border-[2.5px] border-[#1e293b] bg-white/25" /><div className="h-12 w-3 rounded-full border-[2px] border-[#1e293b] bg-white/35" /></>;
  if (row === 1) return <><div className="h-8 w-10 rounded-b-xl border-[2.5px] border-[#1e293b] bg-white/25" /><div className="h-11 w-4 rounded-b-full border-[2.5px] border-[#1e293b] bg-white/25" /></>;
  return <><div className="h-7 w-9 border-[2.5px] border-[#1e293b] bg-white/25 [clip-path:polygon(20%_0,80%_0,100%_100%,0_100%)]" /><div className="h-3 w-12 rounded-full border-[2.5px] border-[#1e293b] bg-white/30" /></>;
}
