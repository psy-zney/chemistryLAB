import { ReactNode } from "react";

export function CartoonButton({
  children,
  onClick,
  variant = "white",
  className = "",
  glow = false,
  ariaLabel,
}: {
  children: ReactNode;
  onClick?: () => void;
  variant?: "white" | "blue" | "green" | "red" | "purple";
  className?: string;
  glow?: boolean;
  ariaLabel?: string;
}) {
  const bg: Record<string, string> = {
    white: "bg-white text-[var(--lab-stroke)]",
    blue: "bg-[var(--lab-blue)] text-white",
    green: "bg-[var(--dollar-green)] text-white",
    red: "bg-[var(--danger-red)] text-white",
    purple: "bg-[var(--diamond-purple)] text-white",
  };
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className={`cartoon-stroke rounded-xl px-4 py-2 cartoon-shadow transition-all duration-150 active:translate-y-1 active:shadow-none hover:-translate-y-0.5 ${bg[variant]} ${glow ? "lab-button-glow" : ""} ${className}`}
    >
      {children}
    </button>
  );
}

export function CurrencyPill({ icon, value, tint }: { icon: string; value: string | number; tint: string }) {
  return (
    <div className="cartoon-stroke rounded-full bg-white cartoon-shadow px-3 py-1 flex items-center gap-2">
      <span className="grid place-items-center rounded-full size-6" style={{ background: tint }}>
        {icon}
      </span>
      <span style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}>{value}</span>
    </div>
  );
}

// Animated cartoon Erlenmeyer flask. fill: 0..1, bubbling optional
export function Flask({ color = "#38BDF8", fill = 0.6, bubbling = false, size = 120 }: { color?: string; fill?: number; bubbling?: boolean; size?: number }) {
  const liquidY = 150 - fill * 90;
  return (
    <svg width={size} height={size} viewBox="0 0 120 160" fill="none">
      <defs>
        <clipPath id="flaskClip">
          <path d="M48 20 L48 62 L20 132 Q16 148 32 148 L88 148 Q104 148 100 132 L72 62 L72 20 Z" />
        </clipPath>
      </defs>
      {/* liquid */}
      <g clipPath="url(#flaskClip)">
        <rect x="0" y={liquidY} width="120" height="160" fill={color} />
        <rect x="0" y={liquidY} width="120" height="6" fill="rgba(255,255,255,0.5)" />
        {bubbling && (
          <>
            <circle cx="45" cy="120" r="4" fill="rgba(255,255,255,0.6)">
              <animate attributeName="cy" values="140;90" dur="1.4s" repeatCount="indefinite" />
              <animate attributeName="opacity" values="0.8;0" dur="1.4s" repeatCount="indefinite" />
            </circle>
            <circle cx="65" cy="120" r="3" fill="rgba(255,255,255,0.6)">
              <animate attributeName="cy" values="140;85" dur="1.1s" repeatCount="indefinite" />
              <animate attributeName="opacity" values="0.8;0" dur="1.1s" repeatCount="indefinite" />
            </circle>
            <circle cx="55" cy="120" r="2.5" fill="rgba(255,255,255,0.6)">
              <animate attributeName="cy" values="145;95" dur="1.7s" repeatCount="indefinite" />
              <animate attributeName="opacity" values="0.8;0" dur="1.7s" repeatCount="indefinite" />
            </circle>
          </>
        )}
      </g>
      {/* glass outline */}
      <path d="M48 20 L48 62 L20 132 Q16 148 32 148 L88 148 Q104 148 100 132 L72 62 L72 20 Z" stroke="#1E293B" strokeWidth="4" strokeLinejoin="round" />
      {/* cork */}
      <rect x="44" y="8" width="32" height="16" rx="4" fill="#C08457" stroke="#1E293B" strokeWidth="4" />
      <rect x="44" y="18" width="32" height="6" fill="#1E293B" opacity="0.15" />
    </svg>
  );
}
