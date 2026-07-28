// Game data: chemicals, tools, shop items, quests

export type Phase = "liquid" | "solid" | "gas";

export interface Chemical {
  id: string;
  name: string;
  formula: string;
  color: string; // hex of the substance
  phase: Phase;
  amount: number; // owned amount
  unit: "g" | "ml";
  realWorld: string; // real-life application
}

export const CHEMICALS: Chemical[] = [
  { id: "nacl", name: "Muối ăn", formula: "NaCl", color: "#E2E8F0", phase: "solid", amount: 500, unit: "g", realWorld: "Gia vị nấu ăn, sản xuất NaOH và Cl₂ bằng điện phân." },
  { id: "h2o", name: "Nước cất", formula: "H₂O", color: "#7DD3FC", phase: "liquid", amount: 1000, unit: "ml", realWorld: "Dung môi vạn năng, làm mát và pha loãng dung dịch." },
  { id: "hcl", name: "Axit clohidric", formula: "HCl", color: "#FDE68A", phase: "liquid", amount: 250, unit: "ml", realWorld: "Tẩy rửa kim loại, sản xuất trong dạ dày để tiêu hóa." },
  { id: "naoh", name: "Xút", formula: "NaOH", color: "#F1F5F9", phase: "solid", amount: 200, unit: "g", realWorld: "Sản xuất xà phòng, thông cống, xử lý giấy." },
  { id: "cuso4", name: "Đồng sunfat", formula: "CuSO₄", color: "#38BDF8", phase: "solid", amount: 120, unit: "g", realWorld: "Diệt nấm cho cây trồng, mạ điện đồng." },
  { id: "h2so4", name: "Axit sunfuric", formula: "H₂SO₄", color: "#FCD34D", phase: "liquid", amount: 300, unit: "ml", realWorld: "Ắc quy xe, phân bón, được gọi là 'máu của công nghiệp'." },
  { id: "kmno4", name: "Thuốc tím", formula: "KMnO₄", color: "#A855F7", phase: "solid", amount: 80, unit: "g", realWorld: "Sát khuẩn vết thương, xử lý nước." },
  { id: "bacl2", name: "Bari clorua", formula: "BaCl₂", color: "#FEF3C7", phase: "solid", amount: 60, unit: "g", realWorld: "Nhận biết ion sunfat, tạo pháo hoa màu xanh lục." },
];

// Simple reaction rules keyed by sorted reactant ids
export interface Reaction {
  reactants: string[];
  productName: string;
  productFormula: string;
  color: string;
  effect: "precipitate" | "gas" | "heat" | "colorchange";
  note: string;
  danger?: boolean;
}

export const REACTIONS: Reaction[] = [
  { reactants: ["hcl", "naoh"], productName: "Muối & Nước", productFormula: "NaCl + H₂O", color: "#BAE6FD", effect: "heat", note: "Phản ứng trung hòa toả nhiệt mạnh!" },
  { reactants: ["bacl2", "h2so4"], productName: "Bari sunfat", productFormula: "BaSO₄↓", color: "#F8FAFC", effect: "precipitate", note: "Kết tủa trắng lắng xuống đáy." },
  { reactants: ["cuso4", "naoh"], productName: "Đồng hidroxit", productFormula: "Cu(OH)₂↓", color: "#60A5FA", effect: "precipitate", note: "Kết tủa xanh lơ đặc trưng." },
  { reactants: ["cuso4", "nacl"], productName: "Dung dịch trộn", productFormula: "hỗn hợp", color: "#38BDF8", effect: "colorchange", note: "Chỉ hoà tan, không phản ứng rõ rệt." },
  { reactants: ["hcl", "kmno4"], productName: "Khí Clo", productFormula: "Cl₂↑", color: "#D9F99D", effect: "gas", note: "Sinh khí clo độc — coi chừng!", danger: true },
];

export interface ShopItem {
  id: string;
  name: string;
  formula?: string;
  price: number;
  currency: "dollar" | "diamond";
  category: "base" | "rare" | "machine" | "skin";
  emoji: string;
}

export const SHOP_ITEMS: ShopItem[] = [
  { id: "fe3o4", name: "Oxit sắt từ", formula: "Fe₃O₄", price: 120, currency: "dollar", category: "base", emoji: "🧲" },
  { id: "cus", name: "Đồng sunfua", formula: "CuS", price: 90, currency: "dollar", category: "base", emoji: "🪨" },
  { id: "al2o3", name: "Nhôm oxit", formula: "Al₂O₃", price: 150, currency: "dollar", category: "base", emoji: "⚪" },
  { id: "pt", name: "Bạch kim", formula: "Pt", price: 40, currency: "diamond", category: "rare", emoji: "✨" },
  { id: "au", name: "Vàng nguyên chất", formula: "Au", price: 60, currency: "diamond", category: "rare", emoji: "🥇" },
  { id: "f2", name: "Khí Flo", formula: "F₂", price: 35, currency: "diamond", category: "rare", emoji: "☁️" },
  { id: "electro", name: "Máy điện phân", price: 800, currency: "dollar", category: "machine", emoji: "⚡" },
  { id: "ostwald", name: "Máy sản xuất HNO₃", formula: "Ostwald", price: 1200, currency: "dollar", category: "machine", emoji: "🏭" },
  { id: "oxit-sep", name: "Máy tách Oxit", price: 950, currency: "dollar", category: "machine", emoji: "🔬" },
  { id: "blouse-blue", name: "Áo blouse xanh", price: 15, currency: "diamond", category: "skin", emoji: "🥼" },
  { id: "goggles", name: "Kính bảo hộ neon", price: 20, currency: "diamond", category: "skin", emoji: "🥽" },
  { id: "hair-punk", name: "Kiểu tóc punk", price: 25, currency: "diamond", category: "skin", emoji: "💇" },
];

export interface Quest {
  id: string;
  npc: string;
  avatar: string;
  dialogue: string;
  need: string;
  rewardDollar: number;
  rewardExp: number;
  stars: number;
}

export const QUESTS: Quest[] = [
  { id: "q1", npc: "Bác sĩ Lan", avatar: "👩‍⚕️", dialogue: "Cần 50g dd NaCl 0.9% để sát khuẩn vết thương.", need: "NaCl · 50g · 0.9%", rewardDollar: 250, rewardExp: 40, stars: 3 },
  { id: "q2", npc: "Nông dân Tư", avatar: "👨‍🌾", dialogue: "Tôi cần CuSO₄ để diệt nấm cho vườn cà chua.", need: "CuSO₄ · 100g", rewardDollar: 180, rewardExp: 30, stars: 2 },
  { id: "q3", npc: "Thợ kim hoàn Mai", avatar: "💍", dialogue: "Pha giúp tôi dung dịch mạ vàng Au tinh khiết nhé!", need: "Au · 5g · 99%", rewardDollar: 500, rewardExp: 80, stars: 3 },
  { id: "q4", npc: "Kỹ sư Hùng", avatar: "👷", dialogue: "Cần H₂SO₄ đậm đặc cho dây chuyền ắc quy.", need: "H₂SO₄ · 200ml", rewardDollar: 320, rewardExp: 55, stars: 2 },
];
