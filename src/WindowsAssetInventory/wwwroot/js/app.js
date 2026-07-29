const elements = {
  rows: document.querySelector("#asset-rows"),
  count: document.querySelector("#result-count"),
  search: document.querySelector("#search"),
  type: document.querySelector("#type"),
  status: document.querySelector("#status"),
  refresh: document.querySelector("#refresh"),
  dialog: document.querySelector("#create-dialog"),
  form: document.querySelector("#create-form"),
  formError: document.querySelector("#form-error"),
  toast: document.querySelector("#toast")
};

const statusClass = value => value.toLowerCase().replace(" ", "-");

const escapeHtml = value => String(value ?? "")
  .replaceAll("&", "&amp;")
  .replaceAll("<", "&lt;")
  .replaceAll(">", "&gt;")
  .replaceAll('"', "&quot;")
  .replaceAll("'", "&#039;");

const friendlyDate = value => value
  ? new Intl.DateTimeFormat("en", { day: "2-digit", month: "short", year: "numeric" })
    .format(new Date(`${value}T00:00:00Z`))
  : "Not recorded";

const warrantyClass = value => {
  if (!value) return "";
  const remaining = (new Date(`${value}T00:00:00Z`) - new Date()) / 86400000;
  return remaining >= 0 && remaining <= 30 ? "warranty-soon" : "";
};

async function request(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const validation = problem.errors
      ? Object.values(problem.errors).flat().join(" ")
      : "";
    throw new Error(validation || problem.detail || problem.title || `Request failed: ${response.status}`);
  }
  return response.status === 204 ? null : response.json();
}

async function loadDashboard() {
  const dashboard = await request("/api/dashboard");
  document.querySelector("#stat-total").textContent = dashboard.totalAssets;
  document.querySelector("#stat-assigned").textContent = dashboard.assigned;
  document.querySelector("#stat-stock").textContent = dashboard.inStock;
  document.querySelector("#stat-warranty").textContent = dashboard.expiringWarranty;
  document.querySelector("#stat-assigned-detail").textContent =
    `${dashboard.assignedUsers} people with equipment`;
}

async function loadAssets() {
  elements.rows.innerHTML = '<tr><td colspan="6" class="empty">Loading inventory…</td></tr>';
  const parameters = new URLSearchParams();
  if (elements.search.value.trim()) parameters.set("search", elements.search.value.trim());
  if (elements.type.value) parameters.set("type", elements.type.value);
  if (elements.status.value) parameters.set("status", elements.status.value);
  const assets = await request(`/api/assets?${parameters}`);
  elements.count.textContent = `${assets.length} asset${assets.length === 1 ? "" : "s"} shown`;

  if (assets.length === 0) {
    elements.rows.innerHTML = '<tr><td colspan="6" class="empty">No assets match the current filters.</td></tr>';
    return;
  }

  elements.rows.innerHTML = assets.map(asset => `
    <tr>
      <td class="asset-name">
        <strong>${escapeHtml(asset.assetTag)}</strong>
        <small>${escapeHtml(asset.serialNumber)}</small>
      </td>
      <td>${escapeHtml(asset.type)}</td>
      <td>
        <strong>${escapeHtml(asset.manufacturer)} ${escapeHtml(asset.model)}</strong>
        <div class="device-sub">${escapeHtml(asset.hostname || "No hostname")}</div>
      </td>
      <td>${escapeHtml(asset.assignedUserName || "Unassigned")}</td>
      <td class="${warrantyClass(asset.warrantyUntil)}">${friendlyDate(asset.warrantyUntil)}</td>
      <td><span class="status ${statusClass(asset.status)}">${escapeHtml(asset.status)}</span></td>
    </tr>`).join("");
}

async function refreshAll() {
  try {
    await Promise.all([loadDashboard(), loadAssets()]);
  } catch (error) {
    elements.rows.innerHTML = `<tr><td colspan="6" class="empty">${escapeHtml(error.message)}</td></tr>`;
  }
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.classList.add("visible");
  window.setTimeout(() => elements.toast.classList.remove("visible"), 2800);
}

let searchTimer;
elements.search.addEventListener("input", () => {
  window.clearTimeout(searchTimer);
  searchTimer = window.setTimeout(loadAssets, 220);
});
elements.type.addEventListener("change", loadAssets);
elements.status.addEventListener("change", loadAssets);
elements.refresh.addEventListener("click", refreshAll);
document.querySelector("#open-create").addEventListener("click", () => elements.dialog.showModal());
document.querySelector("#close-create").addEventListener("click", () => elements.dialog.close());
document.querySelector("#cancel-create").addEventListener("click", () => elements.dialog.close());

elements.form.addEventListener("submit", async event => {
  event.preventDefault();
  elements.formError.textContent = "";
  const form = new FormData(elements.form);
  const payload = Object.fromEntries(form.entries());
  payload.purchaseDate ||= null;
  payload.warrantyUntil ||= null;
  payload.hostname ||= null;

  try {
    await request("/api/assets", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    elements.form.reset();
    elements.dialog.close();
    showToast("Asset added to inventory.");
    await refreshAll();
  } catch (error) {
    elements.formError.textContent = error.message;
  }
});

refreshAll();
