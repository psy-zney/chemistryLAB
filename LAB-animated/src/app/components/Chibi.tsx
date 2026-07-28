// Reusable chibi scientist. The animated treatment is opt-in so the character editor stays calm.
export function Chibi({
  hair = "#3B2A1E",
  coat = "#FFFFFF",
  goggles = "#00A8E8",
  gloves = "#2ECC71",
  size = 180,
  animated = false,
  wave = false,
  className = "",
}: {
  hair?: string;
  coat?: string;
  goggles?: string;
  gloves?: string;
  size?: number;
  animated?: boolean;
  wave?: boolean;
  className?: string;
}) {
  return (
    <svg width={size} height={size} viewBox="0 0 200 220" fill="none" className={`${animated ? "chibi--animated" : ""} ${wave ? "chibi--wave" : ""} ${className}`}>
      {/* body / coat */}
      <path d="M55 210 L55 140 Q55 110 100 110 Q145 110 145 140 L145 210 Z" fill={coat} stroke="#1E293B" strokeWidth="5" strokeLinejoin="round" />
      <line x1="100" y1="115" x2="100" y2="210" stroke="#1E293B" strokeWidth="3" />
      {/* gloves / hands */}
      <circle cx="52" cy="175" r="14" fill={gloves} stroke="#1E293B" strokeWidth="5" />
      <g className="chibi-wave-arm" transform="rotate(-8 148 175)"><circle cx="148" cy="175" r="14" fill={gloves} stroke="#1E293B" strokeWidth="5" /></g>
      {/* neck */}
      <rect x="88" y="98" width="24" height="20" fill="#F2C6A0" stroke="#1E293B" strokeWidth="4" />
      {/* head */}
      <circle cx="100" cy="70" r="42" fill="#F7D3AE" stroke="#1E293B" strokeWidth="5" />
      {/* hair */}
      <path d="M58 62 Q60 22 100 22 Q140 22 142 62 Q120 44 100 46 Q80 44 58 62 Z" fill={hair} stroke="#1E293B" strokeWidth="5" strokeLinejoin="round" />
      {/* goggles */}
      <circle cx="84" cy="70" r="13" fill={goggles} fillOpacity="0.6" stroke="#1E293B" strokeWidth="4" />
      <circle cx="116" cy="70" r="13" fill={goggles} fillOpacity="0.6" stroke="#1E293B" strokeWidth="4" />
      <line x1="97" y1="70" x2="103" y2="70" stroke="#1E293B" strokeWidth="4" />
      <ellipse className="chibi-eye" cx="84" cy="70" rx="3" ry="4" fill="#1E293B" />
      <ellipse className="chibi-eye" cx="116" cy="70" rx="3" ry="4" fill="#1E293B" />
      <path className="chibi-goggle-shine" d="M78 63 L86 58 M110 63 L118 58" stroke="white" strokeWidth="3" strokeLinecap="round" opacity="0.85" />
      {/* smile */}
      <path d="M88 90 Q100 100 112 90" stroke="#1E293B" strokeWidth="4" fill="none" strokeLinecap="round" />
    </svg>
  );
}
