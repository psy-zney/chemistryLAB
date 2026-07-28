import { ReactNode, useState } from "react";
import { X } from "lucide-react";
import { motion } from "motion/react";
import { CartoonButton } from "./ui-kit";
import { Chibi } from "./Chibi";
import { SHOP_ITEMS, QUESTS, Chemical } from "../game/data";

export function Modal({ title, onClose, children, wide = false }: { title: string; onClose: () => void; children: ReactNode; wide?: boolean }) {
  return (
    <motion.div className="fixed inset-0 z-50 grid place-items-center bg-[var(--lab-stroke)]/40 backdrop-blur-sm p-4" onClick={onClose} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }} role="presentation">
      <motion.div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`bg-white cartoon-stroke rounded-3xl cartoon-shadow w-full ${wide ? "max-w-4xl" : "max-w-2xl"} max-h-[88vh] flex flex-col overflow-hidden`}
        onClick={(e) => e.stopPropagation()}
        initial={{ opacity: 0, y: 18, scale: 0.98 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 10, scale: 0.98 }}
        transition={{ duration: 0.24, ease: [0.16, 1, 0.3, 1] }}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b-[3px] border-[var(--lab-stroke)] bg-[var(--secondary)]">
          <h2 className="text-[var(--lab-stroke)]" style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}>{title}</h2>
          <button type="button" aria-label="Đóng cửa sổ" onClick={onClose} className="cartoon-stroke rounded-full size-9 grid place-items-center bg-white hover:bg-[var(--danger-red)] hover:text-white transition-colors">
            <X size={18} />
          </button>
        </div>
        <div className="p-6 overflow-y-auto">{children}</div>
      </motion.div>
    </motion.div>
  );
}

const SHOP_TABS = [
  { id: "base", label: "Hoá chất Base" },
  { id: "rare", label: "Đặc biệt 💎" },
  { id: "machine", label: "Máy móc" },
  { id: "skin", label: "Trang phục" },
] as const;

export function ShopModal({ onClose, onBuy }: { onClose: () => void; onBuy: (price: number, currency: "dollar" | "diamond", name: string) => void }) {
  const [tab, setTab] = useState<(typeof SHOP_TABS)[number]["id"]>("base");
  const items = SHOP_ITEMS.filter((i) => i.category === tab);
  return (
    <Modal title="CỬA HÀNG HOÁ CHẤT & MÁY MÓC" onClose={onClose} wide>
      <div className="flex gap-2 flex-wrap mb-5">
        {SHOP_TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`cartoon-stroke rounded-xl px-3 py-1.5 transition-colors ${tab === t.id ? "bg-[var(--lab-blue)] text-white" : "bg-white text-[var(--lab-stroke)]"}`}
            style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}
          >
            {t.label}
          </button>
        ))}
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
        {items.map((it) => (
          <div key={it.id} className="cartoon-stroke rounded-2xl p-4 flex flex-col items-center text-center gap-2 bg-[#F8FAFC] cartoon-shadow">
            <div className="text-4xl">{it.emoji}</div>
            <div style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>{it.name}</div>
            {it.formula && <div className="text-[var(--muted-foreground)] text-sm">{it.formula}</div>}
            <CartoonButton variant={it.currency === "diamond" ? "purple" : "green"} className="mt-1 text-sm" onClick={() => onBuy(it.price, it.currency, it.name)}>
              {it.currency === "diamond" ? "💎" : "$"} {it.price} · MUA
            </CartoonButton>
          </div>
        ))}
      </div>
    </Modal>
  );
}

export function InventoryModal({ onClose, chemicals }: { onClose: () => void; chemicals: Chemical[] }) {
  const [selected, setSelected] = useState<Chemical | null>(null);
  return (
    <Modal title="KHO HOÁ CHẤT" onClose={onClose} wide>
      <div className="grid grid-cols-3 sm:grid-cols-4 gap-4">
        {chemicals.map((c) => (
          <button
            key={c.id}
            onClick={() => setSelected(c)}
            className={`cartoon-stroke rounded-2xl p-3 flex flex-col items-center gap-1 cartoon-shadow transition-transform hover:-translate-y-0.5 ${selected?.id === c.id ? "bg-[var(--secondary)]" : "bg-[#F8FAFC]"}`}
          >
            <div className="size-9 rounded-full cartoon-stroke" style={{ background: c.color }} />
            <div style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>{c.formula}</div>
            <div className="text-xs text-[var(--muted-foreground)]">{c.amount}{c.unit}</div>
          </button>
        ))}
      </div>
      {selected && (
        <div className="mt-5 cartoon-stroke rounded-2xl p-4 bg-[var(--secondary)]">
          <div style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}>{selected.name} · {selected.formula}</div>
          <p className="mt-1 text-sm"><b>Sơ đồ chuyển hoá:</b> {selected.formula} → có thể tạo hợp chất mới qua phản ứng trên bàn lab.</p>
          <p className="mt-2 text-sm"><b>Ứng dụng thực tế:</b> {selected.realWorld}</p>
        </div>
      )}
    </Modal>
  );
}

function Stars({ n }: { n: number }) {
  return <span>{"⭐".repeat(n)}{"☆".repeat(3 - n)}</span>;
}

export function QuestsModal({ onClose, onComplete }: { onClose: () => void; onComplete: (dollar: number, exp: number) => void }) {
  const [done, setDone] = useState<string[]>([]);
  return (
    <Modal title="NHIỆM VỤ ĐẶT HÀNG" onClose={onClose} wide>
      <div className="flex flex-col gap-4">
        {QUESTS.map((q) => {
          const finished = done.includes(q.id);
          return (
            <div key={q.id} className="cartoon-stroke rounded-2xl p-4 flex items-center gap-4 bg-[#F8FAFC] cartoon-shadow">
              <div className="text-4xl">{q.avatar}</div>
              <div className="flex-1">
                <div style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>{q.npc}</div>
                <p className="text-sm text-[var(--muted-foreground)]">"{q.dialogue}"</p>
                <div className="text-sm mt-1">Yêu cầu: <b>{q.need}</b></div>
                <div className="text-sm">Thưởng: <span className="text-[var(--dollar-green)]" style={{ fontWeight: 700 }}>${q.rewardDollar}</span> · 🌟 {q.rewardExp} EXP · <Stars n={q.stars} /></div>
              </div>
              <CartoonButton
                variant={finished ? "white" : "blue"}
                onClick={() => {
                  if (!finished) {
                    setDone([...done, q.id]);
                    onComplete(q.rewardDollar, q.rewardExp);
                  }
                }}
                className="text-sm"
              >
                {finished ? "✓ Hoàn thành" : "Giao hàng"}
              </CartoonButton>
            </div>
          );
        })}
      </div>
    </Modal>
  );
}

const HAIRS = ["#3B2A1E", "#111827", "#D97706", "#E11D48", "#7C3AED"];
const COATS = ["#FFFFFF", "#BAE6FD", "#FBCFE8", "#DCFCE7"];
const GLOVES = ["#2ECC71", "#00A8E8", "#E74C3C", "#9B59B6"];
const GOGGLES = ["#00A8E8", "#2ECC71", "#F59E0B", "#9B59B6"];

export function CharacterModal({ onClose }: { onClose: () => void }) {
  const [hair, setHair] = useState(HAIRS[0]);
  const [coat, setCoat] = useState(COATS[0]);
  const [gloves, setGloves] = useState(GLOVES[0]);
  const [goggles, setGoggles] = useState(GOGGLES[0]);

  const Swatches = ({ label, colors, value, set }: { label: string; colors: string[]; value: string; set: (c: string) => void }) => (
    <div>
      <div className="text-sm mb-1" style={{ fontFamily: "var(--font-display)", fontWeight: 600 }}>{label}</div>
      <div className="flex gap-2">
        {colors.map((c) => (
          <button key={c} onClick={() => set(c)} className={`size-8 rounded-full cartoon-stroke ${value === c ? "ring-4 ring-[var(--lab-blue)]" : ""}`} style={{ background: c }} />
        ))}
      </div>
    </div>
  );

  return (
    <Modal title="TÙY CHỈNH NHÂN VẬT" onClose={onClose} wide>
      <div className="grid md:grid-cols-2 gap-6">
        <div className="grid place-items-center cartoon-stroke rounded-2xl bg-[var(--secondary)] p-4">
          <Chibi hair={hair} coat={coat} gloves={gloves} goggles={goggles} size={220} />
        </div>
        <div className="flex flex-col gap-4 justify-center">
          <Swatches label="Tóc" colors={HAIRS} value={hair} set={setHair} />
          <Swatches label="Kính bảo hộ" colors={GOGGLES} value={goggles} set={setGoggles} />
          <Swatches label="Áo Blouse" colors={COATS} value={coat} set={setCoat} />
          <Swatches label="Găng tay" colors={GLOVES} value={gloves} set={setGloves} />
          <CartoonButton variant="green" onClick={onClose} className="mt-2">Lưu diện mạo</CartoonButton>
        </div>
      </div>
    </Modal>
  );
}

export function IncidentPopup({ note, penalty, onClose }: { note: string; penalty: number; onClose: () => void }) {
  return (
    <motion.div className="fixed inset-0 z-[60] grid place-items-center bg-black/70 p-4" onClick={onClose} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }}>
      <motion.div role="alertdialog" aria-modal="true" aria-label="Sự cố thí nghiệm" className="cartoon-stroke rounded-3xl bg-white max-w-md w-full p-6 text-center cartoon-shadow" onClick={(e) => e.stopPropagation()} initial={{ opacity: 0, y: 16, scale: 0.98 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, y: 8, scale: 0.98 }} transition={{ duration: 0.24, ease: [0.16, 1, 0.3, 1] }}>
        <div className="text-6xl mb-2">☠️💥</div>
        <h2 className="text-[var(--danger-red)]" style={{ fontFamily: "var(--font-display)", fontWeight: 700 }}>THÍ NGHIỆM THẤT BẠI!</h2>
        <p className="mt-2">{note}</p>
        <div className="mt-4 cartoon-stroke rounded-xl bg-[#FEF2F2] p-3 text-[var(--danger-red)]" style={{ fontWeight: 700 }}>
          Hoá đơn bồi thường phòng Lab: -${penalty}
        </div>
        <CartoonButton variant="red" onClick={onClose} className="mt-4">Đã hiểu</CartoonButton>
      </motion.div>
    </motion.div>
  );
}
