import { CHEMICAL_BY_ID, ChemicalSpec } from "./chemistryCatalogue";

export type ReactionEffect = "none" | "heat" | "precipitate" | "gas" | "color";
export type LabStation = "workbench" | "fume-hood" | "sink" | "storage" | "analysis";

export interface VesselAddition {
  chemicalId: string;
  amount: number;
  unit: "g" | "mL";
  moles: number;
}

export interface ReactionRule {
  id: string;
  reactants: Record<string, number>;
  equation: string;
  productName: string;
  productFormula: string;
  productMolarMass: number;
  productColor: string;
  effect: ReactionEffect;
  enthalpyNote: string;
  temperatureDelta: number;
  efficiency: number;
  hoodRequired?: boolean;
  catalyst?: string;
  safety: string;
}

export interface SimulationResult {
  status: "empty" | "mixture" | "reaction" | "blocked";
  title: string;
  equation: string;
  observation: string;
  effect: ReactionEffect;
  color: string;
  temperatureC: number;
  limitingReagent?: string;
  theoreticalYieldG?: number;
  estimatedYieldG?: number;
  safety: string;
  reactionId?: string;
}

const REACTION_RULES: ReactionRule[] = [
  {
    id: "neutralize-hcl-naoh",
    reactants: { hcl: 1, naoh: 1 },
    equation: "HCl + NaOH → NaCl + H₂O",
    productName: "Natri clorua và nước",
    productFormula: "NaCl",
    productMolarMass: 58.44,
    productColor: "#cfe9ee",
    effect: "heat",
    enthalpyNote: "Trung hòa axit mạnh–bazơ mạnh, tỏa nhiệt.",
    temperatureDelta: 12,
    efficiency: 0.98,
    safety: "Thành cốc ấm lên; tránh bắn dung dịch ăn mòn.",
  },
  {
    id: "copper-hydroxide",
    reactants: { cuso4: 1, naoh: 2 },
    equation: "CuSO₄ + 2NaOH → Cu(OH)₂↓ + Na₂SO₄",
    productName: "Đồng(II) hiđroxit",
    productFormula: "Cu(OH)₂",
    productMolarMass: 97.56,
    productColor: "#4b9ed1",
    effect: "precipitate",
    enthalpyNote: "Trao đổi ion tạo chất ít tan.",
    temperatureDelta: 1,
    efficiency: 0.94,
    safety: "Thu gom kết tủa chứa đồng vào bình chất thải kim loại nặng.",
  },
  {
    id: "barium-sulfate",
    reactants: { bacl2: 1, h2so4: 1 },
    equation: "BaCl₂ + H₂SO₄ → BaSO₄↓ + 2HCl",
    productName: "Bari sunfat",
    productFormula: "BaSO₄",
    productMolarMass: 233.39,
    productColor: "#e7e8e2",
    effect: "precipitate",
    enthalpyNote: "Kết tủa trắng hình thành nhanh.",
    temperatureDelta: 1,
    efficiency: 0.96,
    safety: "Bari clorua hòa tan độc; không tiếp xúc trực tiếp.",
  },
  {
    id: "silver-chloride",
    reactants: { agno3: 1, nacl: 1 },
    equation: "AgNO₃ + NaCl → AgCl↓ + NaNO₃",
    productName: "Bạc clorua",
    productFormula: "AgCl",
    productMolarMass: 143.32,
    productColor: "#e4e1cf",
    effect: "precipitate",
    enthalpyNote: "AgCl kết tủa trắng, nhạy sáng.",
    temperatureDelta: 0,
    efficiency: 0.95,
    safety: "Che ánh sáng và thu hồi toàn bộ chất thải bạc.",
  },
  {
    id: "lead-iodide",
    reactants: { pbno3: 1, ki: 2 },
    equation: "Pb(NO₃)₂ + 2KI → PbI₂↓ + 2KNO₃",
    productName: "Chì(II) iodua",
    productFormula: "PbI₂",
    productMolarMass: 461.01,
    productColor: "#e1bd27",
    effect: "precipitate",
    enthalpyNote: "Kết tủa vàng đặc trưng.",
    temperatureDelta: 1,
    efficiency: 0.91,
    hoodRequired: true,
    safety: "Hợp chất chì độc tích lũy; chỉ thao tác trong tủ hút và hệ kín.",
  },
  {
    id: "iron-hydroxide",
    reactants: { fecl3: 1, naoh: 3 },
    equation: "FeCl₃ + 3NaOH → Fe(OH)₃↓ + 3NaCl",
    productName: "Sắt(III) hiđroxit",
    productFormula: "Fe(OH)₃",
    productMolarMass: 106.87,
    productColor: "#8f4f28",
    effect: "precipitate",
    enthalpyNote: "Kết tủa keo nâu đỏ.",
    temperatureDelta: 1,
    efficiency: 0.92,
    safety: "Tránh dư bazơ và thu gom bùn kết tủa.",
  },
  {
    id: "zinc-hydrogen",
    reactants: { zn: 1, hcl: 2 },
    equation: "Zn + 2HCl → ZnCl₂ + H₂↑",
    productName: "Kẽm clorua và hiđro",
    productFormula: "H₂",
    productMolarMass: 2.016,
    productColor: "#d8e6e7",
    effect: "gas",
    enthalpyNote: "Kim loại đẩy hiđro khỏi axit, tỏa nhiệt nhẹ.",
    temperatureDelta: 7,
    efficiency: 0.9,
    hoodRequired: true,
    safety: "Hiđro dễ cháy; tắt mọi nguồn lửa và dùng tủ hút.",
  },
  {
    id: "magnesium-hydrogen",
    reactants: { mg: 1, hcl: 2 },
    equation: "Mg + 2HCl → MgCl₂ + H₂↑",
    productName: "Magie clorua và hiđro",
    productFormula: "H₂",
    productMolarMass: 2.016,
    productColor: "#d8e6e7",
    effect: "gas",
    enthalpyNote: "Phản ứng nhanh, sủi bọt mạnh và tỏa nhiệt.",
    temperatureDelta: 18,
    efficiency: 0.94,
    hoodRequired: true,
    safety: "Hiđro dễ cháy; phản ứng có thể mạnh, dùng lượng nhỏ trong tủ hút.",
  },
  {
    id: "carbonate-acid",
    reactants: { caco3: 1, hcl: 2 },
    equation: "CaCO₃ + 2HCl → CaCl₂ + CO₂↑ + H₂O",
    productName: "Canxi clorua và cacbon đioxit",
    productFormula: "CO₂",
    productMolarMass: 44.01,
    productColor: "#d7e5e5",
    effect: "gas",
    enthalpyNote: "Cacbonat giải phóng CO₂ khi gặp axit.",
    temperatureDelta: 2,
    efficiency: 0.95,
    safety: "Không đậy kín bình vì khí sinh ra làm tăng áp suất.",
  },
  {
    id: "peroxide-decomposition",
    reactants: { h2o2: 2, mno2: 0.02 },
    equation: "2H₂O₂ —MnO₂→ 2H₂O + O₂↑",
    productName: "Nước và oxi",
    productFormula: "O₂",
    productMolarMass: 32,
    productColor: "#d8e8e9",
    effect: "gas",
    enthalpyNote: "MnO₂ xúc tác phân hủy H₂O₂, phản ứng tỏa nhiệt.",
    temperatureDelta: 10,
    efficiency: 0.9,
    catalyst: "mno2",
    safety: "Oxi làm tăng cháy; tránh chất dễ cháy và không đậy kín.",
  },
  {
    id: "weak-acid-neutralization",
    reactants: { ch3cooh: 1, naoh: 1 },
    equation: "CH₃COOH + NaOH → CH₃COONa + H₂O",
    productName: "Natri axetat và nước",
    productFormula: "CH₃COONa",
    productMolarMass: 82.03,
    productColor: "#d7e9e7",
    effect: "heat",
    enthalpyNote: "Trung hòa axit yếu với bazơ mạnh.",
    temperatureDelta: 7,
    efficiency: 0.97,
    safety: "Thêm bazơ từ từ và theo dõi nhiệt độ.",
  },
  {
    id: "ammonium-chloride",
    reactants: { nh3: 1, hcl: 1 },
    equation: "NH₃ + HCl → NH₄Cl",
    productName: "Amoni clorua",
    productFormula: "NH₄Cl",
    productMolarMass: 53.49,
    productColor: "#e5e7df",
    effect: "heat",
    enthalpyNote: "Amoniac nhận proton tạo ion amoni.",
    temperatureDelta: 9,
    efficiency: 0.96,
    hoodRequired: true,
    safety: "Hơi HCl và NH₃ đều kích ứng mạnh; thao tác trong tủ hút.",
  },
  {
    id: "copper-silver",
    reactants: { cu: 1, agno3: 2 },
    equation: "Cu + 2AgNO₃ → Cu(NO₃)₂ + 2Ag↓",
    productName: "Bạc kim loại và đồng(II) nitrat",
    productFormula: "Ag",
    productMolarMass: 107.87,
    productColor: "#aeb5b3",
    effect: "color",
    enthalpyNote: "Phản ứng thế oxi hóa–khử; bạc bám lên đồng, dung dịch chuyển xanh.",
    temperatureDelta: 2,
    efficiency: 0.88,
    safety: "Thu hồi bạc và dung dịch đồng; tránh tiếp xúc AgNO₃.",
  },
];

export const MISSION_REACTIONS = [
  { id: "copper-hydroxide", title: "Tạo kết tủa xanh Cu(OH)₂", target: "Cu(OH)₂", reward: 80 },
  { id: "carbonate-acid", title: "Thu khí CO₂ từ cacbonat", target: "CO₂", reward: 65 },
  { id: "silver-chloride", title: "Nhận biết ion Cl⁻ bằng AgNO₃", target: "AgCl", reward: 95 },
];

export function quantityToMoles(chemical: ChemicalSpec, amount: number, unit: "g" | "mL") {
  if (!Number.isFinite(amount) || amount <= 0) return 0;
  if (unit === "mL" && chemical.concentrationM) {
    return (amount / 1000) * chemical.concentrationM;
  }
  if (unit === "g") return (amount * chemical.purity) / chemical.molarMass;
  const density = Number.parseFloat(chemical.density.replace(",", "."));
  return ((amount * (Number.isFinite(density) ? density : 1)) * chemical.purity) / chemical.molarMass;
}

export function makeAddition(chemical: ChemicalSpec, amount: number): VesselAddition {
  return {
    chemicalId: chemical.id,
    amount,
    unit: chemical.defaultUnit,
    moles: quantityToMoles(chemical, amount, chemical.defaultUnit),
  };
}

function findReaction(additions: VesselAddition[]) {
  const ids = new Set(additions.map((item) => item.chemicalId));
  return REACTION_RULES.find((rule) =>
    Object.keys(rule.reactants).every((reactantId) => ids.has(reactantId)),
  );
}

function chemicalName(id: string) {
  return CHEMICAL_BY_ID[id]?.formula ?? id;
}

export function simulateVessel(
  additions: VesselAddition[],
  baseTemperatureC: number,
  station: LabStation,
): SimulationResult {
  if (additions.length === 0) {
    return {
      status: "empty",
      title: "Cốc phản ứng sạch",
      equation: "—",
      observation: "Chọn một chất từ thư viện và định lượng trước khi nạp.",
      effect: "none",
      color: "#cbdde0",
      temperatureC: baseTemperatureC,
      safety: "Kính và găng đang được kiểm tra.",
    };
  }

  const reaction = findReaction(additions);
  const lastChemical = CHEMICAL_BY_ID[additions.at(-1)!.chemicalId];

  if (!reaction) {
    const labels = additions.map((addition) => chemicalName(addition.chemicalId)).join(" + ");
    return {
      status: "mixture",
      title: additions.length === 1 ? lastChemical.name : "Hỗn hợp chưa có phản ứng xác định",
      equation: labels,
      observation:
        additions.length === 1
          ? `${lastChemical.appearance}. Chưa có chất phản ứng thứ hai.`
          : "Các chất đang trộn hoặc hòa tan; engine không suy diễn phản ứng khi chưa có quy tắc cân bằng.",
      effect: "color",
      color: lastChemical.model.albedo,
      temperatureC: baseTemperatureC,
      safety: lastChemical.handling,
    };
  }

  if (reaction.hoodRequired && station !== "fume-hood") {
    return {
      status: "blocked",
      title: "Điều kiện thao tác không an toàn",
      equation: reaction.equation,
      observation: "Phản ứng đã bị khóa trước khi nạp chất cuối cùng.",
      effect: "none",
      color: lastChemical.model.albedo,
      temperatureC: baseTemperatureC,
      safety: `${reaction.safety} Di chuyển tới tủ hút rồi thử lại.`,
      reactionId: reaction.id,
    };
  }

  const molesById = additions.reduce<Record<string, number>>((acc, addition) => {
    acc[addition.chemicalId] = (acc[addition.chemicalId] ?? 0) + addition.moles;
    return acc;
  }, {});
  const limiting = Object.entries(reaction.reactants).reduce(
    (lowest, [id, coefficient]) => {
      const available = (molesById[id] ?? 0) / coefficient;
      return available < lowest.available ? { id, available } : lowest;
    },
    { id: "", available: Number.POSITIVE_INFINITY },
  );
  const theoreticalYieldG = limiting.available * reaction.productMolarMass;
  const estimatedYieldG = theoreticalYieldG * reaction.efficiency;

  return {
    status: "reaction",
    title: reaction.productName,
    equation: reaction.equation,
    observation: `${reaction.enthalpyNote} Hiệu suất mô phỏng ${(reaction.efficiency * 100).toFixed(0)}%.`,
    effect: reaction.effect,
    color: reaction.productColor,
    temperatureC: Math.min(120, baseTemperatureC + reaction.temperatureDelta),
    limitingReagent: chemicalName(limiting.id),
    theoreticalYieldG,
    estimatedYieldG,
    safety: reaction.safety,
    reactionId: reaction.id,
  };
}

