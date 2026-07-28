import { useEffect, useState } from "react";
import { Flask } from "./ui-kit";

const WATERMARKS = ["H₂O", "NaCl", "H₂SO₄", "KMnO₄", "CuSO₄", "NaOH", "HCl", "BaSO₄", "Cl₂", "Fe₃O₄"];

export function LoadingScreen({ onDone }: { onDone: () => void }) {
  const [pct, setPct] = useState(0);

  useEffect(() => {
    const t = setInterval(() => {
      setPct((p) => {
        if (p >= 100) {
          clearInterval(t);
          setTimeout(onDone, 500);
          return 100;
        }
        return Math.min(100, p + Math.random() * 12);
      });
    }, 180);
    return () => clearInterval(t);
  }, [onDone]);

  return (
    <div className="relative size-full grid place-items-center bg-[#F1F5F9] overflow-hidden">
      {/* watermark formulas */}
      <div className="absolute inset-0 opacity-[0.08] select-none pointer-events-none">
        {WATERMARKS.map((f, i) => (
          <span
            key={f + i}
            className="absolute"
            style={{
              left: `${(i * 37) % 90}%`,
              top: `${(i * 53) % 90}%`,
              fontFamily: "var(--font-display)",
              fontSize: `${28 + (i % 3) * 14}px`,
              transform: `rotate(${(i % 5) * 12 - 20}deg)`,
            }}
          >
            {f}
          </span>
        ))}
      </div>

      <div className="relative flex flex-col items-center gap-6">
        <Flask color="#38BDF8" fill={pct / 100} bubbling size={150} />

        {/* test-tube progress bar */}
        <div className="w-72 h-8 rounded-full bg-white cartoon-stroke overflow-hidden relative">
          <div
            className="h-full transition-all duration-200"
            style={{ width: `${pct}%`, background: "linear-gradient(90deg,#00A8E8,#3A86FF)" }}
          />
          <span
            className="absolute inset-0 grid place-items-center text-[var(--lab-stroke)]"
            style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}
          >
            {Math.round(pct)}%
          </span>
        </div>

        <p className="text-[var(--lab-blue-deep)]" style={{ fontWeight: 700 }}>
          Đang chuẩn bị dụng cụ thí nghiệm...
        </p>
      </div>
    </div>
  );
}
