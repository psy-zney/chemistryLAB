import {
  ComponentType,
  CSSProperties,
  FormEvent,
  MutableRefObject,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { Canvas, useFrame, useThree } from "@react-three/fiber";
import * as THREE from "three";
import {
  ArrowLeft,
  Beaker,
  BookOpen,
  Check,
  ChevronRight,
  CircleGauge,
  Eraser,
  FlaskConical,
  Focus,
  Hand,
  Info,
  ListChecks,
  LockKeyhole,
  MousePointer2,
  Move,
  Pause,
  Play,
  Search,
  ShieldCheck,
  TestTube2,
  Thermometer,
  TriangleAlert,
  X,
} from "lucide-react";
import {
  CHEMICAL_BY_ID,
  CHEMISTRY_CATALOGUE,
  ChemicalSpec,
  HAZARD_LABELS,
} from "../game/chemistryCatalogue";
import {
  LabStation,
  MISSION_REACTIONS,
  SimulationResult,
  VesselAddition,
  makeAddition,
  simulateVessel,
} from "../game/chemistryEngine";

const SCENE = {
  ceiling: "#dce8ee",
  wall: "#d4e2e9",
  wallSecondary: "#c4d5dd",
  floor: "#9cabb2",
  floorLine: "#758991",
  graphite: "#17212b",
  graphiteRaised: "#22303b",
  steel: "#87969d",
  steelDark: "#4d5f68",
  bench: "#dbe5e7",
  benchTop: "#edf2f1",
  cobalt: "#286ee8",
  cyan: "#54b9d0",
  safe: "#4cb98a",
  warm: "#d89a45",
  warning: "#c95d48",
  glass: "#b9e1e8",
  glassHighlight: "#e7f7f7",
  skin: "#bf8668",
  glove: "#78cfc9",
  coat: "#eef3f2",
  darkLiquid: "#24343c",
} as const;

const ZONE_LABELS: Record<LabStation, string> = {
  workbench: "Bàn phản ứng",
  "fume-hood": "Tủ hút khí độc",
  sink: "Bồn rửa",
  storage: "Kho hóa chất",
  analysis: "Bàn phân tích",
};

type InspectorTab = "chemicals" | "vessel" | "missions";

interface DesktopLabGameProps {
  onExit: () => void;
  onIncident: (note: string) => void;
  onReward: () => void;
}

interface PlayerRigProps {
  active: boolean;
  selected: ChemicalSpec;
  onZoneChange: (zone: LabStation) => void;
}

interface LabWorldProps {
  selected: ChemicalSpec;
  additions: VesselAddition[];
  simulation: SimulationResult;
  playerActive: boolean;
  onZoneChange: (zone: LabStation) => void;
}

export function DesktopLabGame({
  onExit,
  onIncident,
  onReward,
}: DesktopLabGameProps) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const pointerTargetRef = useRef<HTMLCanvasElement | null>(null);
  const [sessionStarted, setSessionStarted] = useState(false);
  const [playerActive, setPlayerActive] = useState(false);
  const [tab, setTab] = useState<InspectorTab>("chemicals");
  const [selectedId, setSelectedId] = useState("cuso4");
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<"all" | ChemicalSpec["category"]>("all");
  const [amount, setAmount] = useState(10);
  const [temperature, setTemperature] = useState(24);
  const [station, setStation] = useState<LabStation>("workbench");
  const [additions, setAdditions] = useState<VesselAddition[]>([]);
  const [safetyOverride, setSafetyOverride] = useState<SimulationResult | null>(null);
  const [log, setLog] = useState<string[]>([
    "Hệ thống đã hiệu chuẩn cân, nhiệt kế và cảm biến khí.",
  ]);
  const [completedMissions, setCompletedMissions] = useState<string[]>([]);
  const [activeMission, setActiveMission] = useState(MISSION_REACTIONS[0].id);

  const selected = CHEMICAL_BY_ID[selectedId];
  const simulation = useMemo(
    () => safetyOverride ?? simulateVessel(additions, temperature, station),
    [additions, safetyOverride, station, temperature],
  );

  const filteredChemicals = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("vi");
    return CHEMISTRY_CATALOGUE.filter((chemical) => {
      const categoryMatch = category === "all" || chemical.category === category;
      const queryMatch =
        normalized.length === 0 ||
        `${chemical.name} ${chemical.formula}`.toLocaleLowerCase("vi").includes(normalized);
      return categoryMatch && queryMatch;
    });
  }, [category, query]);

  useEffect(() => {
    const updatePointerState = () => {
      const locked = document.pointerLockElement === pointerTargetRef.current;
      setPlayerActive(locked);
      if (locked) setSessionStarted(true);
    };
    document.addEventListener("pointerlockchange", updatePointerState);
    return () => document.removeEventListener("pointerlockchange", updatePointerState);
  }, []);

  useEffect(() => {
    const handleKeys = (event: KeyboardEvent) => {
      if (event.code === "Tab" && sessionStarted) {
        event.preventDefault();
        if (playerActive && document.pointerLockElement) {
          document.exitPointerLock();
        } else if (playerActive) {
          setPlayerActive(false);
        } else {
          enterPointerMode();
        }
      }
      if (event.code === "KeyE" && playerActive) {
        document.exitPointerLock();
        if (station === "sink") setTab("vessel");
        else if (station === "storage") setTab("chemicals");
        else setTab("vessel");
      }
    };
    window.addEventListener("keydown", handleKeys);
    return () => window.removeEventListener("keydown", handleKeys);
  }, [playerActive, sessionStarted, station]);

  useEffect(() => {
    if (
      simulation.status === "reaction" &&
      simulation.reactionId === activeMission &&
      !completedMissions.includes(activeMission)
    ) {
      setCompletedMissions((current) => [...current, activeMission]);
      setLog((current) =>
        [`Hoàn thành nhiệm vụ: ${simulation.title}. Phần thưởng đã ghi nhận.`, ...current].slice(0, 6),
      );
      onReward();
    }
  }, [activeMission, completedMissions, onReward, simulation]);

  const enterPointerMode = () => {
    const canvas = pointerTargetRef.current;
    if (!canvas) return;
    setSessionStarted(true);
    try {
      const request = canvas.requestPointerLock();
      if (request && typeof request.catch === "function") {
        request.catch(() => setPlayerActive(true));
      }
    } catch {
      setPlayerActive(true);
    }
  };

  const addSelectedChemical = () => {
    const addition = makeAddition(selected, amount);
    const candidate = [...additions, addition];
    const nextResult = simulateVessel(candidate, temperature, station);
    if (nextResult.status === "blocked") {
      setSafetyOverride(nextResult);
      setLog((current) => [`Đã khóa thao tác: ${nextResult.safety}`, ...current].slice(0, 6));
      onIncident(nextResult.safety);
      return;
    }
    setSafetyOverride(null);
    setAdditions(candidate);
    setLog((current) =>
      [`Nạp ${amount} ${selected.defaultUnit} ${selected.formula} tại ${ZONE_LABELS[station]}.`, ...current].slice(0, 6),
    );
    setTab("vessel");
  };

  const washVessel = () => {
    setAdditions([]);
    setSafetyOverride(null);
    setTemperature(24);
    setLog((current) => ["Cốc đã được rửa và đưa về 24 °C.", ...current].slice(0, 6));
  };

  return (
    <main
      className="desktop-lab"
      data-playing={playerActive}
      aria-label="Mô phỏng phòng thí nghiệm hóa học 3D cho desktop"
    >
      <div className="desktop-lab__mobile-fallback">
        <FlaskConical aria-hidden="true" />
        <h1>Chế độ này cần màn hình desktop</h1>
        <p>
          Mở ở chiều rộng từ 960 px để dùng chuột khóa góc nhìn, bàn phím WASD và bảng
          phân tích hóa học.
        </p>
        <button type="button" className="lab-control lab-control--primary" onClick={onExit}>
          <ArrowLeft size={17} aria-hidden="true" /> Trở về lobby
        </button>
      </div>

      <nav className="desktop-lab__rail" aria-label="Công cụ phòng thí nghiệm">
        <button type="button" className="lab-rail__brand" onClick={onExit} aria-label="Trở về lobby">
          <FlaskConical size={22} aria-hidden="true" />
        </button>
        <div className="lab-rail__tools">
          <RailButton
            label="Thư viện chất"
            icon={BookOpen}
            active={tab === "chemicals"}
            onClick={() => {
              document.exitPointerLock?.();
              setTab("chemicals");
            }}
          />
          <RailButton
            label="Cốc phản ứng"
            icon={Beaker}
            active={tab === "vessel"}
            onClick={() => {
              document.exitPointerLock?.();
              setTab("vessel");
            }}
          />
          <RailButton
            label="Nhiệm vụ"
            icon={ListChecks}
            active={tab === "missions"}
            onClick={() => {
              document.exitPointerLock?.();
              setTab("missions");
            }}
          />
        </div>
        <div className="lab-rail__status" aria-label="Trang bị bảo hộ đã bật">
          <ShieldCheck size={19} aria-hidden="true" />
          <span>PPE</span>
        </div>
      </nav>

      <section className="desktop-lab__viewport" ref={viewportRef} aria-label="Không gian phòng thí nghiệm 3D">
        <header className="desktop-lab__topbar">
          <button type="button" className="lab-back" onClick={onExit}>
            <ArrowLeft size={17} aria-hidden="true" /> Lobby
          </button>
          <div className="lab-readouts" aria-label="Trạng thái mô phỏng">
            <span><Focus size={15} aria-hidden="true" /> {ZONE_LABELS[station]}</span>
            <span><Thermometer size={15} aria-hidden="true" /> {simulation.temperatureC.toFixed(1)} °C</span>
            <span data-safe={simulation.status !== "blocked"}>
              {simulation.status === "blocked" ? <LockKeyhole size={15} /> : <ShieldCheck size={15} />}
              {simulation.status === "blocked" ? "Đã khóa" : "An toàn"}
            </span>
          </div>
        </header>

        <div className="desktop-lab__canvas">
          <Canvas
            shadows="percentage"
            dpr={[1, 1.55]}
            camera={{ fov: 66, near: 0.04, far: 70, position: [0, 1.68, 5.2] }}
            gl={{ antialias: true, alpha: false, powerPreference: "high-performance" }}
            onCreated={({ gl }) => {
              pointerTargetRef.current = gl.domElement;
              gl.toneMapping = THREE.ACESFilmicToneMapping;
              gl.toneMappingExposure = 1.08;
              gl.outputColorSpace = THREE.SRGBColorSpace;
              gl.domElement.setAttribute("aria-label", "Góc nhìn thứ nhất trong phòng thí nghiệm");
            }}
          >
            <LabWorld
              selected={selected}
              additions={additions}
              simulation={simulation}
              playerActive={playerActive}
              onZoneChange={(nextZone) => {
                setStation(nextZone);
                setSafetyOverride(null);
              }}
            />
          </Canvas>
        </div>

        <div className="lab-crosshair" aria-hidden="true"><span /><span /></div>

        <div className="lab-objective">
          <span>Nhiệm vụ đang ghim</span>
          <strong>{MISSION_REACTIONS.find((mission) => mission.id === activeMission)?.title}</strong>
        </div>

        <footer className="desktop-lab__status-line">
          <span><Move size={15} aria-hidden="true" /> WASD di chuyển</span>
          <span><MousePointer2 size={15} aria-hidden="true" /> Chuột quan sát</span>
          <span><Hand size={15} aria-hidden="true" /> E thao tác</span>
          <span><Pause size={15} aria-hidden="true" /> Tab mở bảng</span>
        </footer>

        {!playerActive && (
          <div className="lab-pause" role="dialog" aria-modal="true" aria-labelledby="lab-pause-title">
            <div className="lab-pause__panel">
              <span className="lab-pause__signal" aria-hidden="true" />
              <h1 id="lab-pause-title">
                {sessionStarted ? "Ca trực đang tạm dừng" : "Phòng thí nghiệm 3D"}
              </h1>
              <p>
                {sessionStarted
                  ? "Bảng phân tích đã mở. Tiếp tục khi bạn sẵn sàng di chuyển."
                  : "Đi bộ giữa kho chất, bàn phản ứng, tủ hút và khu phân tích. Mọi thao tác nguy hiểm đều được kiểm tra trước khi mô phỏng."}
              </p>
              <div className="lab-pause__controls" aria-label="Điều khiển">
                <span><kbd>WASD</kbd> Di chuyển</span>
                <span><kbd>E</kbd> Tương tác</span>
                <span><kbd>Tab</kbd> Bảng lab</span>
              </div>
              <button
                type="button"
                className="lab-control lab-control--primary"
                onClick={enterPointerMode}
              >
                <Play size={17} fill="currentColor" aria-hidden="true" />
                {sessionStarted ? "Tiếp tục di chuyển" : "Bắt đầu ca trực"}
              </button>
            </div>
          </div>
        )}
      </section>

      <aside className="lab-inspector" aria-label="Bảng điều khiển hóa học">
        <InspectorHeader tab={tab} onClose={enterPointerMode} />
        {tab === "chemicals" && (
          <ChemicalLibrary
            chemicals={filteredChemicals}
            selected={selected}
            query={query}
            category={category}
            amount={amount}
            onQuery={setQuery}
            onCategory={setCategory}
            onAmount={setAmount}
            onSelect={setSelectedId}
            onAdd={addSelectedChemical}
          />
        )}
        {tab === "vessel" && (
          <VesselPanel
            additions={additions}
            simulation={simulation}
            temperature={temperature}
            station={station}
            log={log}
            onTemperature={setTemperature}
            onWash={washVessel}
            onOpenLibrary={() => setTab("chemicals")}
          />
        )}
        {tab === "missions" && (
          <MissionPanel
            activeMission={activeMission}
            completed={completedMissions}
            onSelect={setActiveMission}
          />
        )}
      </aside>
    </main>
  );
}

function RailButton({
  label,
  icon: Icon,
  active,
  onClick,
}: {
  label: string;
  icon: ComponentType<{ size?: number; "aria-hidden"?: boolean }>;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      className="lab-rail__button"
      data-active={active}
      aria-pressed={active}
      aria-label={label}
      title={label}
      onClick={onClick}
    >
      <Icon size={19} aria-hidden="true" />
    </button>
  );
}

function InspectorHeader({ tab, onClose }: { tab: InspectorTab; onClose: () => void }) {
  const labels: Record<InspectorTab, [string, string]> = {
    chemicals: ["Thư viện chất", "Dữ liệu vật lý và mô hình 3D"],
    vessel: ["Cốc phản ứng", "Cân bằng mol và quan sát"],
    missions: ["Nhiệm vụ", "Bài thực hành có điều kiện"],
  };
  return (
    <header className="lab-inspector__header">
      <div>
        <h2>{labels[tab][0]}</h2>
        <p>{labels[tab][1]}</p>
      </div>
      <button type="button" className="lab-icon-button" onClick={onClose} aria-label="Đóng bảng và tiếp tục">
        <X size={18} aria-hidden="true" />
      </button>
    </header>
  );
}

function ChemicalLibrary({
  chemicals,
  selected,
  query,
  category,
  amount,
  onQuery,
  onCategory,
  onAmount,
  onSelect,
  onAdd,
}: {
  chemicals: ChemicalSpec[];
  selected: ChemicalSpec;
  query: string;
  category: "all" | ChemicalSpec["category"];
  amount: number;
  onQuery: (value: string) => void;
  onCategory: (value: "all" | ChemicalSpec["category"]) => void;
  onAmount: (value: number) => void;
  onSelect: (id: string) => void;
  onAdd: () => void;
}) {
  const submit = (event: FormEvent) => event.preventDefault();
  return (
    <>
      <form className="chemical-search" onSubmit={submit}>
        <label htmlFor="chemical-search">Tìm theo tên hoặc công thức</label>
        <div className="chemical-search__field">
          <Search size={17} aria-hidden="true" />
          <input
            id="chemical-search"
            value={query}
            onChange={(event) => onQuery(event.target.value)}
            placeholder="Ví dụ: CuSO₄"
          />
        </div>
        <label htmlFor="chemical-category">Nhóm chất</label>
        <select
          id="chemical-category"
          value={category}
          onChange={(event) => onCategory(event.target.value as "all" | ChemicalSpec["category"])}
        >
          <option value="all">Tất cả</option>
          <option value="acid">Axit</option>
          <option value="base">Bazơ</option>
          <option value="salt">Muối</option>
          <option value="oxidizer">Chất oxi hóa</option>
          <option value="metal">Kim loại</option>
          <option value="solvent">Dung môi</option>
          <option value="catalyst">Xúc tác</option>
        </select>
      </form>

      <div className="chemical-list" role="listbox" aria-label="Danh sách hóa chất">
        {chemicals.map((chemical) => (
          <button
            type="button"
            key={chemical.id}
            role="option"
            aria-selected={selected.id === chemical.id}
            className="chemical-row"
            data-selected={selected.id === chemical.id}
            onClick={() => onSelect(chemical.id)}
          >
            <span
              className="chemical-row__sample"
              style={{ "--sample-color": chemical.model.albedo } as CSSProperties}
              aria-hidden="true"
            />
            <span className="chemical-row__identity">
              <strong>{chemical.formula}</strong>
              <small>{chemical.name}</small>
            </span>
            <span className="chemical-row__phase">{chemical.phase === "solid" ? "rắn" : "lỏng"}</span>
            <ChevronRight size={16} aria-hidden="true" />
          </button>
        ))}
        {chemicals.length === 0 && (
          <div className="chemical-empty">
            <Search size={20} aria-hidden="true" />
            <p>Không có chất phù hợp. Xóa từ khóa hoặc đổi nhóm chất.</p>
          </div>
        )}
      </div>

      <section className="chemical-detail" aria-labelledby="chemical-detail-title">
        <div className="chemical-detail__title">
          <span
            className="chemical-detail__model"
            style={{ "--sample-color": selected.model.albedo } as CSSProperties}
            aria-hidden="true"
          >
            <i /><i /><i />
          </span>
          <div>
            <h3 id="chemical-detail-title">{selected.formula}</h3>
            <p>{selected.name} · {selected.form}</p>
          </div>
        </div>
        <dl className="chemical-properties">
          <div><dt>Khối lượng mol</dt><dd>{selected.molarMass.toFixed(3)} g/mol</dd></div>
          <div><dt>Khối lượng riêng</dt><dd>{selected.density}</dd></div>
          <div><dt>Nóng chảy</dt><dd>{selected.meltingPoint}</dd></div>
          <div><dt>Sôi / phân hủy</dt><dd>{selected.boilingPoint}</dd></div>
          <div><dt>Ngoại quan</dt><dd>{selected.appearance}</dd></div>
          <div><dt>Độ tan</dt><dd>{selected.solubility}</dd></div>
        </dl>
        <div className="chemical-hazards" aria-label="Cảnh báo">
          {selected.hazards.length === 0 ? (
            <span data-safe="true"><ShieldCheck size={14} aria-hidden="true" /> Nguy cơ thấp</span>
          ) : (
            selected.hazards.map((hazard) => (
              <span key={hazard}><TriangleAlert size={14} aria-hidden="true" /> {HAZARD_LABELS[hazard]}</span>
            ))
          )}
        </div>
      </section>

      <div className="chemical-dose">
        <label htmlFor="chemical-dose">
          Định lượng <strong>{amount} {selected.defaultUnit}</strong>
        </label>
        <input
          id="chemical-dose"
          type="range"
          min="1"
          max={selected.defaultUnit === "g" ? 30 : 50}
          value={amount}
          onChange={(event) => onAmount(Number(event.target.value))}
        />
        <button type="button" className="lab-control lab-control--primary" onClick={onAdd}>
          <TestTube2 size={17} aria-hidden="true" /> Nạp vào cốc
        </button>
      </div>
    </>
  );
}

function VesselPanel({
  additions,
  simulation,
  temperature,
  station,
  log,
  onTemperature,
  onWash,
  onOpenLibrary,
}: {
  additions: VesselAddition[];
  simulation: SimulationResult;
  temperature: number;
  station: LabStation;
  log: string[];
  onTemperature: (value: number) => void;
  onWash: () => void;
  onOpenLibrary: () => void;
}) {
  return (
    <div className="vessel-panel">
      <section className="reaction-readout" data-state={simulation.status}>
        <div className="reaction-readout__status">
          {simulation.status === "blocked" ? <LockKeyhole size={18} /> : <CircleGauge size={18} />}
          <span>{simulation.status === "reaction" ? "Phản ứng đã nhận diện" : simulation.status === "blocked" ? "Khóa an toàn" : "Đang theo dõi"}</span>
        </div>
        <h3>{simulation.title}</h3>
        <code>{simulation.equation}</code>
        <p>{simulation.observation}</p>
      </section>

      <section className="vessel-contents">
        <div className="panel-section-title">
          <h3>Thành phần trong cốc</h3>
          <span>{additions.length} lượt nạp</span>
        </div>
        {additions.length === 0 ? (
          <button type="button" className="vessel-empty" onClick={onOpenLibrary}>
            <Beaker size={22} aria-hidden="true" />
            <span>Cốc đang sạch</span>
            <small>Mở thư viện để chọn chất đầu tiên.</small>
          </button>
        ) : (
          <ol className="vessel-list">
            {additions.map((addition, index) => (
              <li key={`${addition.chemicalId}-${index}`}>
                <span>{index + 1}</span>
                <strong>{CHEMICAL_BY_ID[addition.chemicalId].formula}</strong>
                <small>{addition.amount} {addition.unit}</small>
                <em>{addition.moles.toFixed(4)} mol</em>
              </li>
            ))}
          </ol>
        )}
      </section>

      <section className="yield-sheet">
        <h3>Cân bằng mô phỏng</h3>
        <dl>
          <div><dt>Vị trí</dt><dd>{ZONE_LABELS[station]}</dd></div>
          <div><dt>Chất giới hạn</dt><dd>{simulation.limitingReagent ?? "—"}</dd></div>
          <div><dt>Lý thuyết</dt><dd>{simulation.theoreticalYieldG ? `${simulation.theoreticalYieldG.toFixed(3)} g` : "—"}</dd></div>
          <div><dt>Ước tính thu được</dt><dd>{simulation.estimatedYieldG ? `${simulation.estimatedYieldG.toFixed(3)} g` : "—"}</dd></div>
        </dl>
      </section>

      <section className="temperature-control">
        <label htmlFor="lab-temperature">
          <Thermometer size={16} aria-hidden="true" /> Nhiệt độ nền
          <strong>{temperature} °C</strong>
        </label>
        <input
          id="lab-temperature"
          type="range"
          min="5"
          max="95"
          value={temperature}
          onChange={(event) => onTemperature(Number(event.target.value))}
        />
      </section>

      <div className="safety-note" data-danger={simulation.status === "blocked"}>
        {simulation.status === "blocked" ? <TriangleAlert size={18} /> : <ShieldCheck size={18} />}
        <p>{simulation.safety}</p>
      </div>

      <section className="experiment-log">
        <h3>Nhật ký gần nhất</h3>
        <ol>
          {log.map((entry, index) => <li key={`${entry}-${index}`}>{entry}</li>)}
        </ol>
      </section>

      <button type="button" className="lab-control lab-control--secondary" onClick={onWash} disabled={additions.length === 0}>
        <Eraser size={17} aria-hidden="true" /> Rửa cốc
      </button>
    </div>
  );
}

function MissionPanel({
  activeMission,
  completed,
  onSelect,
}: {
  activeMission: string;
  completed: string[];
  onSelect: (id: string) => void;
}) {
  return (
    <div className="mission-panel">
      <p className="mission-panel__intro">
        Chọn một bài thực hành. Engine chỉ công nhận khi sản phẩm và điều kiện an toàn đều đúng.
      </p>
      <div className="mission-list">
        {MISSION_REACTIONS.map((mission) => {
          const done = completed.includes(mission.id);
          const active = activeMission === mission.id;
          return (
            <button
              type="button"
              key={mission.id}
              className="mission-row"
              data-active={active}
              data-complete={done}
              onClick={() => onSelect(mission.id)}
            >
              <span className="mission-row__mark">
                {done ? <Check size={16} aria-hidden="true" /> : <FlaskConical size={16} aria-hidden="true" />}
              </span>
              <span>
                <strong>{mission.title}</strong>
                <small>Sản phẩm đích: {mission.target}</small>
              </span>
              <span className="mission-row__reward">+{mission.reward}</span>
            </button>
          );
        })}
      </div>
      <div className="mission-protocol">
        <Info size={18} aria-hidden="true" />
        <p>
          Gợi ý: đọc hệ số trong phương trình, đổi lượng chất sang mol và dùng tủ hút khi
          engine báo có khí hoặc chất độc.
        </p>
      </div>
    </div>
  );
}

function LabWorld({
  selected,
  additions,
  simulation,
  playerActive,
  onZoneChange,
}: LabWorldProps) {
  return (
    <>
      <color attach="background" args={[SCENE.graphite]} />
      <fog attach="fog" args={[SCENE.graphite, 12, 28]} />
      <ambientLight intensity={1.25} color={SCENE.ceiling} />
      <directionalLight
        castShadow
        position={[4, 9, 5]}
        intensity={2.4}
        color={SCENE.glassHighlight}
        shadow-mapSize-width={1024}
        shadow-mapSize-height={1024}
        shadow-camera-far={25}
      />
      <pointLight position={[-5, 3.5, 1]} intensity={8} distance={9} color={SCENE.cyan} />
      <pointLight position={[5, 3.2, -3]} intensity={5} distance={8} color={SCENE.warm} />
      <LabRoom additions={additions} simulation={simulation} />
      <PlayerRig active={playerActive} selected={selected} onZoneChange={onZoneChange} />
    </>
  );
}

function PlayerRig({ active, selected, onZoneChange }: PlayerRigProps) {
  const { camera } = useThree();
  const keys = useRef(new Set<string>());
  const yaw = useRef(0);
  const pitch = useRef(-0.04);
  const lastZone = useRef<LabStation>("workbench");
  const walking = useRef(false);
  const velocity = useMemo(() => new THREE.Vector3(), []);
  const forward = useMemo(() => new THREE.Vector3(), []);
  const right = useMemo(() => new THREE.Vector3(), []);

  useEffect(() => {
    camera.position.set(0, 1.68, 5.2);
    camera.rotation.order = "YXZ";
  }, [camera]);

  useEffect(() => {
    const down = (event: KeyboardEvent) => keys.current.add(event.code);
    const up = (event: KeyboardEvent) => keys.current.delete(event.code);
    const look = (event: MouseEvent) => {
      if (!active) return;
      yaw.current -= event.movementX * 0.0018;
      pitch.current = THREE.MathUtils.clamp(pitch.current - event.movementY * 0.0015, -1.18, 1.12);
    };
    window.addEventListener("keydown", down);
    window.addEventListener("keyup", up);
    document.addEventListener("mousemove", look);
    return () => {
      window.removeEventListener("keydown", down);
      window.removeEventListener("keyup", up);
      document.removeEventListener("mousemove", look);
    };
  }, [active]);

  useFrame((_, delta) => {
    camera.rotation.set(pitch.current, yaw.current, 0, "YXZ");
    velocity.set(0, 0, 0);
    if (active) {
      forward.set(-Math.sin(yaw.current), 0, -Math.cos(yaw.current));
      right.set(Math.cos(yaw.current), 0, -Math.sin(yaw.current));
      if (keys.current.has("KeyW") || keys.current.has("ArrowUp")) velocity.add(forward);
      if (keys.current.has("KeyS") || keys.current.has("ArrowDown")) velocity.sub(forward);
      if (keys.current.has("KeyD") || keys.current.has("ArrowRight")) velocity.add(right);
      if (keys.current.has("KeyA") || keys.current.has("ArrowLeft")) velocity.sub(right);
      walking.current = velocity.lengthSq() > 0;
      if (walking.current) {
        const speed = keys.current.has("ShiftLeft") ? 4.1 : 2.55;
        velocity.normalize().multiplyScalar(speed * Math.min(delta, 0.05));
        const candidateX = THREE.MathUtils.clamp(camera.position.x + velocity.x, -7.15, 7.15);
        const candidateZ = THREE.MathUtils.clamp(camera.position.z + velocity.z, -5.9, 6.1);
        if (!hitsWorkbench(candidateX, camera.position.z)) camera.position.x = candidateX;
        if (!hitsWorkbench(camera.position.x, candidateZ)) camera.position.z = candidateZ;
      }
    } else {
      walking.current = false;
    }

    const nextZone = zoneFor(camera.position);
    if (nextZone !== lastZone.current) {
      lastZone.current = nextZone;
      onZoneChange(nextZone);
    }
  });

  return <ScientistHands selected={selected} walking={walking} />;
}

function hitsWorkbench(x: number, z: number) {
  return Math.abs(x) < 2.8 && z > -1.55 && z < 1.55;
}

function zoneFor(position: THREE.Vector3): LabStation {
  if (position.z < -3.75) return "fume-hood";
  if (position.x < -4.25) return "storage";
  if (position.x > 4.15 && position.z > 0.5) return "sink";
  if (position.x > 4.15) return "analysis";
  return "workbench";
}

function ScientistHands({
  selected,
  walking,
}: {
  selected: ChemicalSpec;
  walking: MutableRefObject<boolean>;
}) {
  const { camera, clock } = useThree();
  const group = useRef<THREE.Group>(null);

  useFrame(() => {
    if (!group.current) return;
    const bob = walking.current ? Math.sin(clock.elapsedTime * 9) * 0.018 : Math.sin(clock.elapsedTime * 1.5) * 0.003;
    group.current.position.copy(camera.position);
    group.current.quaternion.copy(camera.quaternion);
    group.current.translateZ(-0.66);
    group.current.translateY(-0.28 + bob);
  });

  return (
    <group ref={group} scale={0.62} renderOrder={20}>
      <group position={[-0.31, -0.02, 0.03]} rotation={[0.12, -0.1, -0.25]}>
        <mesh castShadow rotation={[0, 0, 0.18]} position={[0, -0.06, 0.08]}>
          <cylinderGeometry args={[0.085, 0.105, 0.45, 18]} />
          <meshStandardMaterial color={SCENE.coat} roughness={0.7} depthTest={false} />
        </mesh>
        <mesh position={[0.02, 0.18, 0.04]} scale={[1, 0.7, 1.3]}>
          <sphereGeometry args={[0.11, 20, 14]} />
          <meshStandardMaterial color={SCENE.glove} roughness={0.42} depthTest={false} />
        </mesh>
      </group>
      <group position={[0.32, -0.015, 0.02]} rotation={[0.1, 0.1, 0.25]}>
        <mesh castShadow rotation={[0, 0, -0.18]} position={[0, -0.06, 0.08]}>
          <cylinderGeometry args={[0.085, 0.105, 0.45, 18]} />
          <meshStandardMaterial color={SCENE.coat} roughness={0.7} depthTest={false} />
        </mesh>
        <mesh position={[-0.02, 0.18, 0.04]} scale={[1, 0.7, 1.3]}>
          <sphereGeometry args={[0.11, 20, 14]} />
          <meshStandardMaterial color={SCENE.glove} roughness={0.42} depthTest={false} />
        </mesh>
        <HeldSample chemical={selected} />
      </group>
    </group>
  );
}

function HeldSample({ chemical }: { chemical: ChemicalSpec }) {
  return (
    <group position={[-0.06, 0.22, -0.02]} rotation={[0.08, 0, -0.08]}>
      <mesh>
        <cylinderGeometry args={[0.075, 0.065, 0.28, 24, 1, true]} />
        <meshPhysicalMaterial
          color={SCENE.glass}
          transmission={0.88}
          thickness={0.12}
          roughness={0.06}
          transparent
          opacity={0.42}
          depthTest={false}
        />
      </mesh>
      {chemical.phase === "liquid" ? (
        <mesh position={[0, -0.055, 0]}>
          <cylinderGeometry args={[0.061, 0.056, 0.15, 24]} />
          <meshPhysicalMaterial
            color={chemical.model.albedo}
            roughness={chemical.model.roughness}
            metalness={chemical.model.metalness}
            transmission={chemical.model.transmission}
            transparent={chemical.model.transmission > 0}
            opacity={0.86}
            depthTest={false}
          />
        </mesh>
      ) : (
        <group position={[0, -0.08, 0]}>
          {SAMPLE_PARTICLES.slice(0, chemical.model.particleShape === "powder" ? 9 : 6).map((point, index) => (
            <mesh key={index} position={[point[0] * 0.055, point[1] * 0.045, point[2] * 0.055]} rotation={[index, index * 0.7, 0]}>
              {chemical.model.particleShape === "crystal" ? (
                <octahedronGeometry args={[0.023, 0]} />
              ) : (
                <sphereGeometry args={[0.022, 10, 8]} />
              )}
              <meshStandardMaterial
                color={chemical.model.albedo}
                roughness={chemical.model.roughness}
                metalness={chemical.model.metalness}
                depthTest={false}
              />
            </mesh>
          ))}
        </group>
      )}
      <mesh position={[0, 0.16, 0]}>
        <cylinderGeometry args={[0.078, 0.078, 0.045, 20]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.6} depthTest={false} />
      </mesh>
    </group>
  );
}

const SAMPLE_PARTICLES: [number, number, number][] = [
  [-0.6, -0.4, 0.2],
  [0.3, -0.5, -0.4],
  [0.6, -0.25, 0.35],
  [-0.15, -0.15, -0.55],
  [-0.45, 0.1, 0.4],
  [0.42, 0.12, 0.05],
  [0.05, 0.32, -0.35],
  [-0.2, 0.45, 0.2],
  [0.55, 0.5, -0.15],
];

function LabRoom({
  additions,
  simulation,
}: {
  additions: VesselAddition[];
  simulation: SimulationResult;
}) {
  return (
    <group>
      <RoomShell />
      <CentralWorkbench additions={additions} simulation={simulation} />
      <ChemicalCabinet position={[-6.35, 0, 0.8]} />
      <AnalysisBench position={[6.15, 0, -1.7]} />
      <SinkStation3D position={[6.1, 0, 2.7]} />
      <FumeHood position={[0, 0, -5.85]} />
      <EmergencyShower position={[-6.7, 0, -4.65]} />
    </group>
  );
}

function RoomShell() {
  const tileLines = useMemo(() => Array.from({ length: 17 }, (_, index) => -8 + index), []);
  const depthLines = useMemo(() => Array.from({ length: 14 }, (_, index) => -6 + index), []);
  return (
    <group>
      <mesh receiveShadow rotation={[-Math.PI / 2, 0, 0]}>
        <planeGeometry args={[16, 13]} />
        <meshStandardMaterial color={SCENE.floor} roughness={0.74} metalness={0.04} />
      </mesh>
      {tileLines.map((x) => (
        <mesh key={`x-${x}`} position={[x, 0.004, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <planeGeometry args={[0.012, 13]} />
          <meshBasicMaterial color={SCENE.floorLine} />
        </mesh>
      ))}
      {depthLines.map((z) => (
        <mesh key={`z-${z}`} position={[0, 0.005, z]} rotation={[-Math.PI / 2, 0, Math.PI / 2]}>
          <planeGeometry args={[0.012, 16]} />
          <meshBasicMaterial color={SCENE.floorLine} />
        </mesh>
      ))}
      <mesh receiveShadow position={[0, 3.4, -6.45]}>
        <boxGeometry args={[16, 6.8, 0.2]} />
        <meshStandardMaterial color={SCENE.wall} roughness={0.83} />
      </mesh>
      <mesh receiveShadow position={[-8.05, 3.4, 0]}>
        <boxGeometry args={[0.2, 6.8, 13]} />
        <meshStandardMaterial color={SCENE.wallSecondary} roughness={0.84} />
      </mesh>
      <mesh receiveShadow position={[8.05, 3.4, 0]}>
        <boxGeometry args={[0.2, 6.8, 13]} />
        <meshStandardMaterial color={SCENE.wallSecondary} roughness={0.84} />
      </mesh>
      <mesh position={[0, 6.75, 0]} rotation={[Math.PI / 2, 0, 0]}>
        <planeGeometry args={[16, 13]} />
        <meshStandardMaterial color={SCENE.ceiling} roughness={0.86} />
      </mesh>
      {[-4.5, 0, 4.5].map((x) => (
        <group key={x} position={[x, 6.52, 0.6]}>
          <mesh rotation={[Math.PI / 2, 0, 0]}>
            <planeGeometry args={[2.7, 0.5]} />
            <meshBasicMaterial color={SCENE.glassHighlight} />
          </mesh>
          <pointLight position={[0, -0.2, 0]} intensity={4.8} distance={7} color={SCENE.glassHighlight} />
        </group>
      ))}
      <group position={[4.6, 3.4, -6.3]}>
        <mesh>
          <boxGeometry args={[4.6, 2.8, 0.08]} />
          <meshPhysicalMaterial color={SCENE.glass} transmission={0.7} roughness={0.18} />
        </mesh>
        {[0, 1, 2].map((index) => (
          <mesh key={index} position={[-1.55 + index * 1.55, 0, 0.08]}>
            <boxGeometry args={[0.05, 2.8, 0.05]} />
            <meshStandardMaterial color={SCENE.steelDark} metalness={0.65} roughness={0.34} />
          </mesh>
        ))}
      </group>
    </group>
  );
}

function CentralWorkbench({
  additions,
  simulation,
}: {
  additions: VesselAddition[];
  simulation: SimulationResult;
}) {
  return (
    <group position={[0, 0, 0]}>
      <mesh castShadow receiveShadow position={[0, 1.05, 0]}>
        <boxGeometry args={[5, 0.18, 2.2]} />
        <meshStandardMaterial color={SCENE.benchTop} roughness={0.3} metalness={0.08} />
      </mesh>
      <mesh castShadow position={[0, 0.58, 0]}>
        <boxGeometry args={[4.75, 0.76, 1.92]} />
        <meshStandardMaterial color={SCENE.bench} roughness={0.54} metalness={0.12} />
      </mesh>
      {[-2.18, 2.18].map((x) => (
        <mesh key={x} castShadow position={[x, 0.34, 0]}>
          <boxGeometry args={[0.12, 0.72, 1.94]} />
          <meshStandardMaterial color={SCENE.steelDark} metalness={0.72} roughness={0.3} />
        </mesh>
      ))}
      <ReactionVessel additions={additions} simulation={simulation} />
      <TestTubeRack3D position={[-1.4, 1.35, 0.15]} />
      <Hotplate position={[1.42, 1.22, 0.08]} active={simulation.effect === "heat"} />
    </group>
  );
}

function ReactionVessel({
  additions,
  simulation,
}: {
  additions: VesselAddition[];
  simulation: SimulationResult;
}) {
  const fill = Math.min(0.7, 0.12 + additions.length * 0.095);
  const liquidHeight = fill * 0.82;
  return (
    <group position={[0, 1.52, 0]}>
      <mesh castShadow>
        <cylinderGeometry args={[0.43, 0.35, 0.92, 48, 1, true]} />
        <meshPhysicalMaterial
          color={SCENE.glass}
          transmission={0.9}
          thickness={0.2}
          roughness={0.07}
          transparent
          opacity={0.36}
          side={THREE.DoubleSide}
        />
      </mesh>
      <mesh position={[0, 0.46, 0]} rotation={[Math.PI / 2, 0, 0]}>
        <torusGeometry args={[0.43, 0.025, 10, 48]} />
        <meshStandardMaterial color={SCENE.glassHighlight} roughness={0.15} />
      </mesh>
      {additions.length > 0 && (
        <mesh position={[0, -0.46 + liquidHeight / 2, 0]}>
          <cylinderGeometry args={[0.4, 0.33, liquidHeight, 48]} />
          <meshPhysicalMaterial
            color={simulation.color}
            roughness={0.2}
            transmission={simulation.effect === "precipitate" ? 0.08 : 0.36}
            transparent
            opacity={0.86}
          />
        </mesh>
      )}
      <ReactionEffect3D effect={simulation.effect} color={simulation.color} active={additions.length > 1} />
      <mesh position={[0, -0.51, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <circleGeometry args={[0.34, 40]} />
        <meshStandardMaterial color={SCENE.darkLiquid} roughness={0.75} transparent opacity={0.22} />
      </mesh>
    </group>
  );
}

function ReactionEffect3D({
  effect,
  color,
  active,
}: {
  effect: SimulationResult["effect"];
  color: string;
  active: boolean;
}) {
  const group = useRef<THREE.Group>(null);
  useFrame(({ clock }) => {
    if (!group.current || !active) return;
    group.current.children.forEach((child, index) => {
      if (effect === "gas" || effect === "heat") {
        child.position.y = ((clock.elapsedTime * (0.16 + index * 0.012) + index * 0.11) % 1.05) - 0.34;
        child.position.x = Math.sin(clock.elapsedTime * 1.7 + index) * 0.2;
      }
    });
  });
  if (!active || (effect !== "gas" && effect !== "heat" && effect !== "precipitate")) return null;
  return (
    <group ref={group}>
      {EFFECT_PARTICLES.map((point, index) => (
        <mesh
          key={index}
          position={[
            point[0] * (effect === "precipitate" ? 0.3 : 0.24),
            effect === "precipitate" ? -0.4 + Math.abs(point[1]) * 0.1 : point[1] * 0.45,
            point[2] * 0.3,
          ]}
        >
          <sphereGeometry args={[effect === "precipitate" ? 0.035 : 0.025 + (index % 3) * 0.007, 10, 8]} />
          <meshStandardMaterial
            color={effect === "precipitate" ? color : SCENE.glassHighlight}
            roughness={effect === "precipitate" ? 0.78 : 0.18}
            transparent
            opacity={effect === "precipitate" ? 0.92 : 0.68}
          />
        </mesh>
      ))}
    </group>
  );
}

const EFFECT_PARTICLES: [number, number, number][] = [
  [-0.8, -0.6, 0.2], [-0.4, 0.2, -0.6], [0.1, -0.1, 0.5], [0.6, 0.5, -0.2],
  [0.8, -0.3, 0.4], [-0.2, 0.7, 0.1], [0.35, -0.7, -0.55], [-0.65, 0.45, 0.6],
  [0.52, 0.08, 0.72], [-0.1, -0.42, -0.75], [0.72, 0.76, 0.1], [-0.73, -0.2, -0.32],
];

function ChemicalCabinet({ position }: { position: [number, number, number] }) {
  const bottles = CHEMISTRY_CATALOGUE.slice(0, 12);
  return (
    <group position={position} rotation={[0, Math.PI / 2, 0]}>
      <mesh castShadow position={[0, 2.15, 0]}>
        <boxGeometry args={[2.9, 4.3, 0.7]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.52} metalness={0.28} />
      </mesh>
      {[0.85, 1.85, 2.85, 3.85].map((y) => (
        <mesh key={y} position={[0, y, 0.4]}>
          <boxGeometry args={[2.72, 0.08, 0.64]} />
          <meshStandardMaterial color={SCENE.steel} metalness={0.62} roughness={0.34} />
        </mesh>
      ))}
      {bottles.map((chemical, index) => {
        const row = Math.floor(index / 4);
        const column = index % 4;
        return (
          <ChemicalBottle3D
            key={chemical.id}
            chemical={chemical}
            position={[-1.02 + column * 0.68, 1.18 + row, 0.78]}
            scale={0.72 + (index % 2) * 0.09}
          />
        );
      })}
      <mesh position={[0, 4.56, 0.1]}>
        <boxGeometry args={[2.9, 0.4, 0.78]} />
        <meshStandardMaterial color={SCENE.cobalt} roughness={0.38} />
      </mesh>
    </group>
  );
}

function ChemicalBottle3D({
  chemical,
  position,
  scale = 1,
}: {
  chemical: ChemicalSpec;
  position: [number, number, number];
  scale?: number;
}) {
  return (
    <group position={position} scale={scale}>
      <mesh castShadow>
        <cylinderGeometry args={[0.22, 0.25, 0.66, 20]} />
        <meshPhysicalMaterial
          color={SCENE.glass}
          transmission={0.62}
          thickness={0.1}
          roughness={0.13}
          transparent
          opacity={0.6}
        />
      </mesh>
      <mesh position={[0, -0.08, 0]}>
        <cylinderGeometry args={[0.195, 0.22, 0.44, 20]} />
        <meshStandardMaterial
          color={chemical.model.albedo}
          roughness={chemical.model.roughness}
          metalness={chemical.model.metalness}
          transparent={chemical.model.transmission > 0}
          opacity={chemical.model.transmission > 0 ? 0.76 : 1}
        />
      </mesh>
      <mesh position={[0, 0.39, 0]}>
        <cylinderGeometry args={[0.18, 0.18, 0.13, 18]} />
        <meshStandardMaterial color={SCENE.graphite} roughness={0.67} />
      </mesh>
      <mesh position={[0, 0.02, 0.251]}>
        <boxGeometry args={[0.32, 0.17, 0.012]} />
        <meshStandardMaterial color={SCENE.benchTop} roughness={0.8} />
      </mesh>
      <mesh position={[0, 0.02, 0.262]}>
        <boxGeometry args={[0.18, 0.025, 0.008]} />
        <meshBasicMaterial color={SCENE.graphite} />
      </mesh>
    </group>
  );
}

function TestTubeRack3D({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      <mesh castShadow position={[0, -0.18, 0]}>
        <boxGeometry args={[1.15, 0.14, 0.42]} />
        <meshStandardMaterial color={SCENE.steelDark} metalness={0.45} roughness={0.4} />
      </mesh>
      {[-0.38, 0, 0.38].map((x, index) => (
        <group key={x} position={[x, 0.16, 0]}>
          <mesh>
            <cylinderGeometry args={[0.1, 0.075, 0.72, 20, 1, true]} />
            <meshPhysicalMaterial color={SCENE.glass} transmission={0.9} roughness={0.05} transparent opacity={0.38} />
          </mesh>
          <mesh position={[0, -0.16, 0]}>
            <cylinderGeometry args={[0.074, 0.06, 0.3, 18]} />
            <meshStandardMaterial color={[SCENE.cyan, SCENE.safe, SCENE.warm][index]} transparent opacity={0.82} />
          </mesh>
        </group>
      ))}
    </group>
  );
}

function Hotplate({ position, active }: { position: [number, number, number]; active: boolean }) {
  return (
    <group position={position}>
      <mesh castShadow>
        <boxGeometry args={[1.1, 0.18, 0.85]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.38} metalness={0.42} />
      </mesh>
      <mesh position={[0, 0.1, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <circleGeometry args={[0.34, 32]} />
        <meshStandardMaterial color={active ? SCENE.warning : SCENE.steelDark} roughness={0.48} emissive={active ? SCENE.warning : SCENE.graphite} emissiveIntensity={active ? 1.2 : 0} />
      </mesh>
      <mesh position={[0.4, 0.04, 0.34]}>
        <sphereGeometry args={[0.055, 14, 10]} />
        <meshBasicMaterial color={active ? SCENE.warning : SCENE.safe} />
      </mesh>
    </group>
  );
}

function AnalysisBench({ position }: { position: [number, number, number] }) {
  return (
    <group position={position} rotation={[0, -Math.PI / 2, 0]}>
      <mesh castShadow position={[0, 0.92, 0]}>
        <boxGeometry args={[3.4, 0.16, 1.15]} />
        <meshStandardMaterial color={SCENE.benchTop} roughness={0.28} metalness={0.12} />
      </mesh>
      <mesh castShadow position={[0, 0.45, 0]}>
        <boxGeometry args={[3.2, 0.78, 1]} />
        <meshStandardMaterial color={SCENE.bench} roughness={0.55} />
      </mesh>
      <group position={[0.45, 1.58, 0]}>
        <mesh castShadow position={[0, 0.35, 0]}>
          <boxGeometry args={[1.55, 1.05, 0.12]} />
          <meshStandardMaterial color={SCENE.graphite} roughness={0.34} />
        </mesh>
        <mesh position={[0, 0.35, 0.07]}>
          <planeGeometry args={[1.35, 0.83]} />
          <meshBasicMaterial color={SCENE.cobalt} />
        </mesh>
        {[0.48, 0.16, -0.16, -0.48].map((x, index) => (
          <mesh key={x} position={[x, 0.14 + index * 0.14, 0.08]}>
            <boxGeometry args={[0.2, 0.025, 0.01]} />
            <meshBasicMaterial color={SCENE.glassHighlight} />
          </mesh>
        ))}
        <mesh position={[0, -0.28, 0]}>
          <boxGeometry args={[0.12, 0.55, 0.12]} />
          <meshStandardMaterial color={SCENE.steelDark} metalness={0.62} />
        </mesh>
      </group>
      <Microscope position={[-1.05, 1.1, 0]} />
    </group>
  );
}

function Microscope({ position }: { position: [number, number, number] }) {
  return (
    <group position={position} scale={0.72}>
      <mesh position={[0, 0.08, 0]}>
        <cylinderGeometry args={[0.42, 0.5, 0.18, 24]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.36} metalness={0.38} />
      </mesh>
      <mesh position={[0, 0.68, 0]} rotation={[0, 0, -0.32]}>
        <cylinderGeometry args={[0.12, 0.16, 1.1, 20]} />
        <meshStandardMaterial color={SCENE.steel} roughness={0.27} metalness={0.72} />
      </mesh>
      <mesh position={[0.17, 1.22, 0]} rotation={[0, 0, -0.32]}>
        <cylinderGeometry args={[0.14, 0.14, 0.46, 20]} />
        <meshStandardMaterial color={SCENE.graphite} roughness={0.42} />
      </mesh>
      <mesh position={[0.05, 0.44, 0]}>
        <boxGeometry args={[0.7, 0.08, 0.56]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} metalness={0.4} />
      </mesh>
    </group>
  );
}

function SinkStation3D({ position }: { position: [number, number, number] }) {
  return (
    <group position={position} rotation={[0, -Math.PI / 2, 0]}>
      <mesh castShadow position={[0, 0.8, 0]}>
        <boxGeometry args={[2.2, 1.6, 1.2]} />
        <meshStandardMaterial color={SCENE.bench} roughness={0.48} metalness={0.14} />
      </mesh>
      <mesh position={[0, 1.62, 0]}>
        <boxGeometry args={[2.25, 0.12, 1.25]} />
        <meshStandardMaterial color={SCENE.steel} roughness={0.24} metalness={0.75} />
      </mesh>
      <mesh position={[0, 1.69, 0]}>
        <boxGeometry args={[1.45, 0.06, 0.72]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.42} metalness={0.42} />
      </mesh>
      <mesh position={[0, 2.1, -0.28]} rotation={[0, 0, Math.PI / 2]}>
        <torusGeometry args={[0.35, 0.055, 12, 28, Math.PI]} />
        <meshStandardMaterial color={SCENE.steel} metalness={0.84} roughness={0.2} />
      </mesh>
    </group>
  );
}

function FumeHood({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      <mesh castShadow position={[0, 1.5, 0]}>
        <boxGeometry args={[4.5, 3, 1.15]} />
        <meshStandardMaterial color={SCENE.graphiteRaised} roughness={0.48} metalness={0.26} />
      </mesh>
      <mesh position={[0, 1.95, 0.61]}>
        <planeGeometry args={[3.95, 1.65]} />
        <meshPhysicalMaterial color={SCENE.glass} transmission={0.78} roughness={0.12} transparent opacity={0.46} />
      </mesh>
      <mesh position={[0, 0.92, 0.62]}>
        <boxGeometry args={[4.1, 0.18, 1]} />
        <meshStandardMaterial color={SCENE.benchTop} roughness={0.31} metalness={0.16} />
      </mesh>
      <mesh position={[0, 3.35, 0]}>
        <boxGeometry args={[4.6, 0.7, 1.18]} />
        <meshStandardMaterial color={SCENE.steelDark} roughness={0.42} metalness={0.52} />
      </mesh>
      <mesh position={[0, 3.35, 0.61]}>
        <boxGeometry args={[1.2, 0.2, 0.04]} />
        <meshBasicMaterial color={SCENE.safe} />
      </mesh>
      <pointLight position={[0, 2.6, 0.35]} intensity={4} distance={4} color={SCENE.glassHighlight} />
    </group>
  );
}

function EmergencyShower({ position }: { position: [number, number, number] }) {
  return (
    <group position={position}>
      <mesh position={[0, 2.6, 0]}>
        <cylinderGeometry args={[0.055, 0.055, 4.7, 14]} />
        <meshStandardMaterial color={SCENE.steel} metalness={0.74} roughness={0.25} />
      </mesh>
      <mesh position={[0.48, 4.9, 0]} rotation={[0, 0, Math.PI / 2]}>
        <cylinderGeometry args={[0.055, 0.055, 0.95, 14]} />
        <meshStandardMaterial color={SCENE.steel} metalness={0.74} roughness={0.25} />
      </mesh>
      <mesh position={[0.92, 4.72, 0]}>
        <coneGeometry args={[0.35, 0.32, 24]} />
        <meshStandardMaterial color={SCENE.safe} metalness={0.32} roughness={0.4} />
      </mesh>
      <mesh position={[-0.28, 2.65, 0.06]}>
        <boxGeometry args={[0.55, 0.75, 0.08]} />
        <meshStandardMaterial color={SCENE.safe} roughness={0.42} />
      </mesh>
    </group>
  );
}
