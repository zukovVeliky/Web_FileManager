(() => {
    const TEXT_EXTENSIONS = new Set([
        'txt','log','md','json','xml','yaml','yml','ini','conf','config','csv','tsv',
        'js','jsx','ts','tsx','html','htm','css','scss','sass','less','cs','vb','java',
        'c','h','cpp','cc','cxx','hpp','py','rb','go','rs','php','sql','sh','ps1','bat'
    ]);
    const IMAGE_EXTENSIONS = new Set(['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp', 'svg']);

    const state = {
        currentPath: '',
        entries: [],
        visibleEntries: [],
        viewMode: 'details',
        searchText: '',
        selectedPath: null,
        selectedPaths: new Set(),
        lastSelectedIndex: -1,
        context: {
            visible: false,
            target: null,
            suppressCloseUntil: 0
        },
        drag: {
            active: false,
            payload: null,
            dropTargetElement: null
        },
        editor: {
            isOpen: false,
            filePath: '',
            fileName: '',
            isDark: false,
            ace: null
        },
        picker: {
            enabled: false,
            onlyImages: false,
            allowedExtensions: null,
            callback: 'ZachyceniURLCKeditor',
            setPath: ''
        },
        root: ''
    };

    const el = {};

    function q(id) {
        return document.getElementById(id);
    }

    function initialize() {
        initializePickerMode();

        el.up = q('rfm-up');
        el.refresh = q('rfm-refresh');
        el.newFolder = q('rfm-new-folder');
        el.upload = q('rfm-upload');
        el.breadcrumb = q('rfm-breadcrumb');
        el.tree = q('rfm-tree');
        el.search = q('rfm-search');
        el.fileInput = q('rfm-file-input');
        el.surface = q('rfm-surface');
        el.table = q('rfm-table');
        el.body = q('rfm-body');
        el.tiles = q('rfm-tiles');
        el.context = q('rfm-context');
        el.viewDetails = q('rfm-view-details');
        el.viewTilesSm = q('rfm-view-tiles-sm');
        el.viewTilesLg = q('rfm-view-tiles-lg');

        el.editorModal = q('rfm-editor-modal');
        el.editorWindow = el.editorModal.querySelector('.code-editor-container');
        el.editorTitle = q('rfm-editor-title');
        el.editorSave = q('rfm-editor-save');
        el.editorTheme = q('rfm-editor-theme');
        el.editorClose = q('rfm-editor-close');
        el.editorLanguage = q('rfm-editor-language');
        el.editorStatus = q('rfm-editor-status');
        el.editorSurface = q('rfm-editor');

        bindEvents();
        loadEntries('');
    }

    function bindEvents() {
        el.up.addEventListener('click', () => navigateUp());
        el.refresh.addEventListener('click', () => loadEntries(state.currentPath));
        el.newFolder.addEventListener('click', () => createFolder());
        el.upload.addEventListener('click', () => el.fileInput.click());
        el.viewDetails.addEventListener('click', () => setViewMode('details'));
        el.viewTilesSm.addEventListener('click', () => setViewMode('tiles-sm'));
        el.viewTilesLg.addEventListener('click', () => setViewMode('tiles-lg'));
        if (el.search) {
            el.search.addEventListener('input', () => {
                state.searchText = (el.search.value || '').trim().toLowerCase();
                renderTable();
            });
        }

        el.fileInput.addEventListener('change', async () => {
            await uploadFiles(el.fileInput.files);
            el.fileInput.value = '';
        });

        el.body.addEventListener('click', onBodyClick);
        el.body.addEventListener('dblclick', onBodyDoubleClick);
        el.body.addEventListener('contextmenu', onBodyContextMenu);
        el.body.addEventListener('dragstart', onBodyDragStart);
        el.body.addEventListener('dragover', onBodyDragOver);
        el.body.addEventListener('drop', onBodyDrop);
        el.body.addEventListener('dragleave', onBodyDragLeave);
        el.body.addEventListener('dragend', onDragEnd);

        el.surface.addEventListener('dragover', onSurfaceDragOver);
        el.surface.addEventListener('drop', onSurfaceDrop);

        el.tiles.addEventListener('click', onTilesClick);
        el.tiles.addEventListener('dblclick', onTilesDoubleClick);
        el.tiles.addEventListener('contextmenu', onTilesContextMenu);
        el.tiles.addEventListener('dragstart', onTilesDragStart);
        el.tiles.addEventListener('dragover', onTilesDragOver);
        el.tiles.addEventListener('drop', onTilesDrop);
        el.tiles.addEventListener('dragleave', onTilesDragLeave);
        el.tiles.addEventListener('dragend', onDragEnd);

        el.surface.addEventListener('contextmenu', (event) => {
            if (event.target.closest('tbody tr')) {
                return;
            }
            event.preventDefault();
            clearSelection();
            openContextMenu(null, event.clientX, event.clientY);
        });

        document.addEventListener('pointerdown', (event) => {
            if (!state.context.visible) {
                return;
            }

            if (Date.now() < state.context.suppressCloseUntil) {
                return;
            }

            if (el.context.contains(event.target)) {
                return;
            }

            closeContextMenu();
        }, true);

        window.addEventListener('resize', () => {
            if (state.context.visible) {
                closeContextMenu();
            }
            clearDropTargetIndicator();
        });

        const swallowAction = (event) => {
            event.preventDefault();
            event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') {
                event.stopImmediatePropagation();
            }
        };

        for (const btn of [el.editorSave, el.editorTheme, el.editorClose]) {
            btn.setAttribute('type', 'button');
            btn.addEventListener('pointerdown', swallowAction, true);
        }

        el.editorSave.addEventListener('click', async (event) => {
            event.preventDefault();
            event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') {
                event.stopImmediatePropagation();
            }
            await saveEditor();
        }, true);

        el.editorTheme.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') {
                event.stopImmediatePropagation();
            }
            toggleEditorTheme();
        }, true);

        el.editorClose.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') {
                event.stopImmediatePropagation();
            }
            closeEditor();
        }, true);

        el.editorLanguage.addEventListener('change', () => applyEditorLanguage(el.editorLanguage.value));

        el.editorModal.addEventListener('click', (event) => {
            if (event.target === el.editorModal) {
                closeEditor();
            }
        });

        // Hard guard against accidental page submit/postback from modal controls.
        document.addEventListener('submit', (event) => {
            if (!state.editor.isOpen) {
                return;
            }

            if (el.editorModal.contains(event.target)) {
                event.preventDefault();
                event.stopPropagation();
            }
        }, true);

        document.addEventListener('keydown', async (event) => {
            if (!state.editor.isOpen) {
                if (isInputLike(event.target)) {
                    return;
                }

                if (event.ctrlKey && event.key.toLowerCase() === 'a') {
                    event.preventDefault();
                    selectAllVisible();
                }
                return;
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                closeEditor();
                return;
            }

            if (event.ctrlKey && event.key.toLowerCase() === 's') {
                event.preventDefault();
                await saveEditor();
            }
        });
    }

    function onBodyClick(event) {
        const row = event.target.closest('tr[data-rel]');
        if (!row) {
            return;
        }

        const rel = row.dataset.rel;
        if (!rel || rel === '__parent__') {
            clearSelection();
            return;
        }

        const index = Number.parseInt(row.dataset.index || '-1', 10);
        updateSelection(rel, index, event.ctrlKey || event.metaKey, event.shiftKey);
    }

    function onBodyDoubleClick(event) {
        const row = event.target.closest('tr[data-rel]');
        if (!row) {
            return;
        }

        if (row.dataset.rel === '__parent__') {
            navigateUp();
            return;
        }

        const entry = state.entries.find((x) => x.relativePath === row.dataset.rel);
        if (!entry) {
            return;
        }

        if (entry.isDirectory) {
            loadEntries(entry.relativePath);
        } else if (state.picker.enabled && canPickEntry(entry)) {
            selectEntryForPicker(entry);
        } else if (isTextFile(entry.name)) {
            openEditor(entry);
        } else {
            downloadEntry(entry);
        }
    }

    function onBodyContextMenu(event) {
        const row = event.target.closest('tr[data-rel]');
        if (!row) {
            return;
        }

        event.preventDefault();
        const rel = row.dataset.rel;
        if (!rel || rel === '__parent__') {
            clearSelection();
            openContextMenu(null, event.clientX, event.clientY);
            return;
        }

        const entry = state.entries.find((x) => x.relativePath === rel) || null;
        if (!entry) {
            return;
        }

        if (!state.selectedPaths.has(rel)) {
            selectSingle(rel, Number.parseInt(row.dataset.index || '-1', 10));
        }

        openContextMenu(entry, event.clientX, event.clientY);
    }

    function getTileFromEvent(event) {
        return event.target.closest('.rfm-tile[data-rel]');
    }

    function onTilesClick(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            clearSelection();
            return;
        }

        const rel = tile.dataset.rel;
        const index = Number.parseInt(tile.dataset.index || '-1', 10);
        updateSelection(rel, index, event.ctrlKey || event.metaKey, event.shiftKey);
    }

    function onTilesDoubleClick(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            return;
        }

        const entry = state.entries.find((x) => x.relativePath === tile.dataset.rel);
        if (!entry) {
            return;
        }

        if (entry.isDirectory) {
            loadEntries(entry.relativePath);
        } else if (state.picker.enabled && canPickEntry(entry)) {
            selectEntryForPicker(entry);
        } else if (isTextFile(entry.name)) {
            openEditor(entry);
        } else {
            downloadEntry(entry);
        }
    }

    function onTilesContextMenu(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            event.preventDefault();
            clearSelection();
            openContextMenu(null, event.clientX, event.clientY);
            return;
        }

        event.preventDefault();
        const rel = tile.dataset.rel;
        const entry = state.entries.find((x) => x.relativePath === rel) || null;
        if (!entry) {
            return;
        }

        if (!state.selectedPaths.has(rel)) {
            selectSingle(rel, Number.parseInt(tile.dataset.index || '-1', 10));
        }

        openContextMenu(entry, event.clientX, event.clientY);
    }

    function onBodyDragStart(event) {
        const row = event.target.closest('tr[data-rel]');
        if (!row || row.dataset.rel === '__parent__') {
            event.preventDefault();
            return;
        }

        const rel = row.dataset.rel;
        const sourceEntry = state.entries.find((x) => x.relativePath === rel);
        if (!sourceEntry) {
            event.preventDefault();
            return;
        }

        if (!state.selectedPaths.has(rel)) {
            selectSingle(rel, Number.parseInt(row.dataset.index || '-1', 10));
        }

        const selectedEntries = getSelectedEntries();
        if (selectedEntries.length === 0) {
            event.preventDefault();
            return;
        }

        state.drag.active = true;
        state.drag.payload = {
            sourcePath: state.currentPath,
            entries: selectedEntries,
            fileNames: selectedEntries.filter((x) => !x.isDirectory).map((x) => x.name),
            directoryNames: selectedEntries.filter((x) => x.isDirectory).map((x) => x.name)
        };

        for (const selectedRow of el.body.querySelectorAll('tr[data-rel].selected')) {
            selectedRow.classList.add('drag-source');
        }

        const count = selectedEntries.length;
        const dragLabel = count === 1 ? selectedEntries[0].name : `${count} položek`;

        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = 'copyMove';
            event.dataTransfer.setData('text/plain', dragLabel);

            const ghost = document.createElement('div');
            ghost.className = 'rfm-drag-ghost';
            ghost.innerHTML = `<i class="bi bi-files"></i><span>${escapeHtml(dragLabel)}</span>`;
            document.body.appendChild(ghost);
            event.dataTransfer.setDragImage(ghost, 16, 16);
            setTimeout(() => ghost.remove(), 0);
        }
    }

    function onTilesDragStart(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            event.preventDefault();
            return;
        }

        const rel = tile.dataset.rel;
        const sourceEntry = state.entries.find((x) => x.relativePath === rel);
        if (!sourceEntry) {
            event.preventDefault();
            return;
        }

        if (!state.selectedPaths.has(rel)) {
            selectSingle(rel, Number.parseInt(tile.dataset.index || '-1', 10));
        }

        const selectedEntries = getSelectedEntries();
        if (selectedEntries.length === 0) {
            event.preventDefault();
            return;
        }

        state.drag.active = true;
        state.drag.payload = {
            sourcePath: state.currentPath,
            entries: selectedEntries,
            fileNames: selectedEntries.filter((x) => !x.isDirectory).map((x) => x.name),
            directoryNames: selectedEntries.filter((x) => x.isDirectory).map((x) => x.name)
        };

        for (const selectedTile of el.tiles.querySelectorAll('.rfm-tile.selected')) {
            selectedTile.classList.add('drag-source');
        }

        const count = selectedEntries.length;
        const dragLabel = count === 1 ? selectedEntries[0].name : `${count} položek`;

        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = 'copyMove';
            event.dataTransfer.setData('text/plain', dragLabel);

            const ghost = document.createElement('div');
            ghost.className = 'rfm-drag-ghost';
            ghost.innerHTML = `<i class="bi bi-files"></i><span>${escapeHtml(dragLabel)}</span>`;
            document.body.appendChild(ghost);
            event.dataTransfer.setDragImage(ghost, 16, 16);
            setTimeout(() => ghost.remove(), 0);
        }
    }

    function onBodyDragOver(event) {
        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        const row = event.target.closest('tr[data-rel]');
        if (!row) {
            return;
        }

        const targetPath = resolveDropPathFromRow(row);
        if (targetPath === null) {
            return;
        }

        event.preventDefault();
        if (event.dataTransfer) {
            event.dataTransfer.dropEffect = (event.ctrlKey || event.metaKey) ? 'copy' : 'move';
        }

        setDropTargetIndicator(row);
    }

    async function onBodyDrop(event) {
        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        const row = event.target.closest('tr[data-rel]');
        if (!row) {
            return;
        }

        const targetPath = resolveDropPathFromRow(row);
        if (targetPath === null) {
            return;
        }

        event.preventDefault();
        await performDrop(targetPath, event.ctrlKey || event.metaKey);
    }

    function onTilesDragOver(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            return;
        }

        const rowLike = {
            dataset: {
                rel: tile.dataset.rel
            }
        };

        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        const targetPath = resolveDropPathFromRow(rowLike);
        if (targetPath === null) {
            return;
        }

        event.preventDefault();
        if (event.dataTransfer) {
            event.dataTransfer.dropEffect = (event.ctrlKey || event.metaKey) ? 'copy' : 'move';
        }

        setDropTargetIndicator(tile);
    }

    async function onTilesDrop(event) {
        const tile = getTileFromEvent(event);
        if (!tile) {
            return;
        }

        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        const rowLike = {
            dataset: {
                rel: tile.dataset.rel
            }
        };

        const targetPath = resolveDropPathFromRow(rowLike);
        if (targetPath === null) {
            return;
        }

        event.preventDefault();
        await performDrop(targetPath, event.ctrlKey || event.metaKey);
    }

    function onTilesDragLeave(event) {
        if (!state.drag.active) {
            return;
        }

        const tile = getTileFromEvent(event);
        if (!tile) {
            return;
        }

        const related = event.relatedTarget;
        if (related && tile.contains(related)) {
            return;
        }

        if (state.drag.dropTargetElement === tile) {
            clearDropTargetIndicator();
        }
    }

    function onBodyDragLeave(event) {
        if (!state.drag.active) {
            return;
        }

        const leavingRow = event.target.closest('tr[data-rel]');
        if (!leavingRow) {
            return;
        }

        const related = event.relatedTarget;
        if (related && leavingRow.contains(related)) {
            return;
        }

        if (state.drag.dropTargetElement === leavingRow) {
            clearDropTargetIndicator();
        }
    }

    function onDragEnd() {
        resetDragState();
    }

    function onSurfaceDragOver(event) {
        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        if (event.target.closest('tr[data-rel]')) {
            return;
        }

        event.preventDefault();
        if (event.dataTransfer) {
            event.dataTransfer.dropEffect = (event.ctrlKey || event.metaKey) ? 'copy' : 'move';
        }

        setDropTargetIndicator(el.surface);
    }

    async function onSurfaceDrop(event) {
        if (!state.drag.active || !state.drag.payload) {
            return;
        }

        if (event.target.closest('tr[data-rel]')) {
            return;
        }

        event.preventDefault();
        await performDrop(state.currentPath, event.ctrlKey || event.metaKey);
    }

    async function api(url, options = null) {
        const response = await fetch(url, options || undefined);
        if (!response.ok) {
            let message = `HTTP ${response.status}`;
            try {
                const json = await response.json();
                if (json && json.message) {
                    message = json.message;
                }
            } catch {
            }
            throw new Error(message);
        }

        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            return response.json();
        }

        return null;
    }

    async function loadEntries(path) {
        closeContextMenu();
        resetDragState();

        state.currentPath = normalizeRelative(path || '');
        clearSelection();

        const encodedPath = encodeURIComponent(encodeUrlParam(state.currentPath));
        const data = await api(`/api/filemanager/list?path=${encodedPath}${buildRootQuerySuffix()}`);
        state.entries = Array.isArray(data) ? data : [];

        renderPath();
        renderTable();
    }

    function renderPath() {
        el.up.disabled = !state.currentPath;
        const rootLabel = getRootLabel();

        const parts = state.currentPath.split('/').filter(Boolean);
        const crumbs = [];
        crumbs.push(`<span class="fm-breadcrumb-item" data-path="">${escapeHtml(rootLabel)}</span>`);
        for (let i = 0; i < parts.length; i++) {
            const partPath = parts.slice(0, i + 1).join('/');
            crumbs.push('<i class="bi bi-chevron-right"></i>');
            crumbs.push(`<span class="fm-breadcrumb-item" data-path="${escapeHtml(partPath)}">${escapeHtml(parts[i])}</span>`);
        }
        el.breadcrumb.innerHTML = crumbs.join('');
        for (const item of el.breadcrumb.querySelectorAll('.fm-breadcrumb-item[data-path]')) {
            item.addEventListener('click', () => {
                loadEntries(item.dataset.path || '');
            });
        }

        const treeRows = [];
        treeRows.push(`<div class="fm-tree-item ${!state.currentPath ? 'active' : ''}" data-path=""><i class="bi bi-folder"></i><span>${escapeHtml(rootLabel)}</span></div>`);
        for (let i = 0; i < parts.length; i++) {
            const partPath = parts.slice(0, i + 1).join('/');
            treeRows.push(`<div class="fm-tree-item ${state.currentPath === partPath ? 'active' : ''}" data-path="${escapeHtml(partPath)}"><i class="bi bi-folder"></i><span>${escapeHtml(parts[i])}</span></div>`);
        }
        for (const dir of state.entries.filter((x) => x.isDirectory)) {
            treeRows.push(`<div class="fm-tree-item child" data-path="${escapeHtml(dir.relativePath)}"><i class="bi bi-folder"></i><span>${escapeHtml(dir.name)}</span></div>`);
        }
        el.tree.innerHTML = treeRows.join('');
        for (const item of el.tree.querySelectorAll('.fm-tree-item[data-path]')) {
            item.addEventListener('click', () => {
                loadEntries(item.dataset.path || '');
            });

            item.addEventListener('dragover', (event) => {
                if (!state.drag.active || !state.drag.payload) {
                    return;
                }

                event.preventDefault();
                event.stopPropagation();
                item.classList.add('drag-target');
                setDropTargetIndicator(item);
                if (event.dataTransfer) {
                    event.dataTransfer.dropEffect = (event.ctrlKey || event.metaKey) ? 'copy' : 'move';
                }
            });

            item.addEventListener('dragleave', () => {
                item.classList.remove('drag-target');
                if (state.drag.dropTargetElement === item) {
                    clearDropTargetIndicator();
                }
            });

            item.addEventListener('drop', async (event) => {
                if (!state.drag.active || !state.drag.payload) {
                    return;
                }

                event.preventDefault();
                event.stopPropagation();
                item.classList.remove('drag-target');
                await performDrop(normalizeRelative(item.dataset.path || ''), event.ctrlKey || event.metaKey);
            });
        }
    }

    function renderTable() {
        const rows = [];
        state.visibleEntries = state.entries.filter((entry) => {
            if (state.picker.enabled && !entry.isDirectory && !isEntryAllowedByPicker(entry)) {
                return false;
            }

            if (!state.searchText) {
                return true;
            }

            return entry.name.toLowerCase().includes(state.searchText);
        });

        if (state.currentPath) {
            rows.push(`
                <tr class="fm-details-row rfm-row" data-rel="__parent__">
                    <td class="fm-col-name">
                        <div class="d-flex align-items-center gap-2"><i class="bi bi-arrow-up-circle-fill" style="color:#0d6efd;"></i><span>..</span></div>
                    </td>
                    <td class="fm-col-size">-</td>
                    <td class="fm-col-date">-</td>
                </tr>
            `);
        }

        for (let i = 0; i < state.visibleEntries.length; i++) {
            const entry = state.visibleEntries[i];
            const iconHtml = entry.isDirectory
                ? '<span class="rfm-icon-chip folder"><i class="bi bi-folder2"></i></span>'
                : getFileVisualHtml(entry);

            const ext = getExtension(entry.name);
            const size = entry.isDirectory ? '-' : formatSize(entry.size || 0);
            const modified = formatDate(entry.modified);
            const selected = state.selectedPaths.has(entry.relativePath) ? ' selected' : '';

            rows.push(`
                <tr class="fm-details-row rfm-row${selected}" data-rel="${escapeHtml(entry.relativePath)}" data-index="${i}" draggable="true">
                    <td class="fm-col-name">
                        <div class="rfm-file-cell">
                            ${iconHtml}
                            <span class="rfm-file-name">${escapeHtml(entry.name)}</span>
                        </div>
                    </td>
                    <td class="fm-col-size">${escapeHtml(size)}</td>
                    <td class="fm-col-date">${escapeHtml(modified)}</td>
                </tr>
            `);
        }

        el.body.innerHTML = rows.join('');
        renderTiles();
        applyViewMode();
    }

    function renderTiles() {
        const tiles = [];
        for (let i = 0; i < state.visibleEntries.length; i++) {
            const entry = state.visibleEntries[i];
            const selected = state.selectedPaths.has(entry.relativePath) ? ' selected' : '';
            const iconHtml = entry.isDirectory
                ? '<span class="rfm-icon-chip folder"><i class="bi bi-folder2"></i></span>'
                : getFileVisualHtml(entry);

            const size = entry.isDirectory ? '' : formatSize(entry.size || 0);
            tiles.push(`
                <div class="rfm-tile${selected}" data-rel="${escapeHtml(entry.relativePath)}" data-index="${i}" draggable="true">
                    <div class="rfm-tile-icon">${iconHtml}</div>
                    <div class="rfm-tile-name">${escapeHtml(entry.name)}</div>
                    <div class="rfm-tile-meta">${escapeHtml(size)}</div>
                </div>
            `);
        }

        el.tiles.innerHTML = tiles.join('');
    }

    function selectRow(relativePath) {
        selectSingle(relativePath, state.visibleEntries.findIndex((x) => x.relativePath === relativePath));
    }

    function setViewMode(mode) {
        state.viewMode = mode;
        applyViewMode();
    }

    function applyViewMode() {
        const isDetails = state.viewMode === 'details';
        const isTilesSm = state.viewMode === 'tiles-sm';
        const isTilesLg = state.viewMode === 'tiles-lg';

        el.table.hidden = !isDetails;
        el.tiles.hidden = isDetails;
        el.tiles.classList.toggle('small', isTilesSm);
        el.tiles.classList.toggle('large', isTilesLg);

        el.viewDetails.classList.toggle('active', isDetails);
        el.viewTilesSm.classList.toggle('active', isTilesSm);
        el.viewTilesLg.classList.toggle('active', isTilesLg);
    }

    function openContextMenu(target, clientX, clientY) {
        state.context.visible = true;
        state.context.target = target;
        state.context.suppressCloseUntil = Date.now() + 180;

        renderContextMenu();

        el.context.hidden = false;
        el.context.style.visibility = 'hidden';
        el.context.style.left = '0px';
        el.context.style.top = '0px';

        const rect = el.context.getBoundingClientRect();
        const margin = 8;
        let x = clientX;
        let y = clientY;

        if (x + rect.width > window.innerWidth - margin) {
            x = window.innerWidth - rect.width - margin;
        }
        if (y + rect.height > window.innerHeight - margin) {
            y = window.innerHeight - rect.height - margin;
        }

        if (x < margin) x = margin;
        if (y < margin) y = margin;

        el.context.style.left = `${x}px`;
        el.context.style.top = `${y}px`;
        el.context.style.visibility = 'visible';
    }

    function closeContextMenu() {
        state.context.visible = false;
        state.context.target = null;
        el.context.hidden = true;
        el.context.style.visibility = '';
        el.context.innerHTML = '';
    }

    function renderContextMenu() {
        const target = state.context.target;
        const selectedEntries = getSelectedEntries();
        const hasMultiSelection = selectedEntries.length > 1;
        const items = [];

        if (!target) {
            items.push({ id: 'new-folder', label: 'Nová složka', icon: 'bi-folder-plus' });
            items.push({ id: 'upload', label: 'Nahrát soubory', icon: 'bi-upload' });
            items.push({ sep: true });
            if (selectedEntries.length > 0) {
                items.push({ id: 'zip', label: 'Komprimovat výběr', icon: 'bi-file-zip' });
                items.push({ id: 'delete', label: 'Smazat výběr', icon: 'bi-trash' });
                items.push({ sep: true });
            }
            items.push({ id: 'refresh', label: 'Obnovit', icon: 'bi-arrow-clockwise' });
        } else if (target.isDirectory) {
            if (!hasMultiSelection) {
                items.push({ id: 'open', label: 'Otevřít', icon: 'bi-folder2-open' });
                items.push({ sep: true });
                items.push({ id: 'rename', label: 'Přejmenovat', icon: 'bi-pencil' });
            }
            items.push({ id: 'zip', label: hasMultiSelection ? 'Komprimovat výběr' : 'Komprimovat', icon: 'bi-file-zip' });
            items.push({ sep: true });
            items.push({ id: 'delete', label: hasMultiSelection ? 'Smazat výběr' : 'Smazat', icon: 'bi-trash' });
        } else {
            if (!hasMultiSelection && state.picker.enabled && canPickEntry(target)) {
                items.push({ id: 'pick', label: 'Vybrat', icon: 'bi-check2-square' });
                items.push({ sep: true });
            }
            if (!hasMultiSelection && isTextFile(target.name)) {
                items.push({ id: 'edit', label: 'Otevřít v editoru', icon: 'bi-file-text' });
            }
            if (!hasMultiSelection) {
                items.push({ id: 'download', label: 'Stáhnout', icon: 'bi-download' });
                if (isZipFile(target.name)) {
                    items.push({ id: 'unzip', label: 'Rozbalit', icon: 'bi-box-arrow-in-down' });
                }
                items.push({ sep: true });
                items.push({ id: 'rename', label: 'Přejmenovat', icon: 'bi-pencil' });
            }
            items.push({ id: 'zip', label: hasMultiSelection ? 'Komprimovat výběr' : 'Komprimovat', icon: 'bi-file-zip' });
            items.push({ sep: true });
            items.push({ id: 'delete', label: hasMultiSelection ? 'Smazat výběr' : 'Smazat', icon: 'bi-trash' });
        }

        el.context.innerHTML = items.map((item) => {
            if (item.sep) {
                return '<div class="context-menu-separator"></div>';
            }
            return `<button type="button" class="context-menu-item" data-action="${item.id}"><i class="bi ${item.icon}"></i>${item.label}</button>`;
        }).join('');

        for (const button of el.context.querySelectorAll('[data-action]')) {
            button.addEventListener('click', async (event) => {
                event.preventDefault();
                event.stopPropagation();

                const action = button.dataset.action;
                await handleContextAction(action);
                closeContextMenu();
            });
        }
    }

    async function handleContextAction(action) {
        const target = state.context.target;

        switch (action) {
            case 'new-folder':
                await createFolder();
                break;
            case 'upload':
                el.fileInput.click();
                break;
            case 'refresh':
                await loadEntries(state.currentPath);
                break;
            case 'open':
                if (target && target.isDirectory) {
                    await loadEntries(target.relativePath);
                }
                break;
            case 'edit':
                {
                    const [single] = resolveActionEntries(target);
                    if (single && isTextFile(single.name)) {
                        await openEditor(single);
                    }
                }
                break;
            case 'download':
                {
                    const [single] = resolveActionEntries(target);
                    if (single) {
                        downloadEntry(single);
                    }
                }
                break;
            case 'pick':
                {
                    const [single] = resolveActionEntries(target);
                    if (single && canPickEntry(single)) {
                        selectEntryForPicker(single);
                    }
                }
                break;
            case 'rename':
                {
                    const [single] = resolveActionEntries(target);
                    if (single) {
                        await renameEntry(single);
                    }
                }
                break;
            case 'delete':
                {
                    const entries = resolveActionEntries(target, true);
                    if (entries.length === 1) {
                        await deleteEntry(entries[0]);
                    } else if (entries.length > 1) {
                        await deleteMultipleEntries(entries);
                    }
                }
                break;
            case 'zip':
                {
                    const entries = resolveActionEntries(target, true);
                    if (entries.length > 0) {
                        await zipEntries(entries);
                    }
                }
                break;
            case 'unzip':
                {
                    const [single] = resolveActionEntries(target);
                    if (single) {
                        await unzipEntry(single);
                    }
                }
                break;
        }
    }

    function navigateUp() {
        if (!state.currentPath) {
            return;
        }

        const parts = state.currentPath.split('/').filter(Boolean);
        parts.pop();
        loadEntries(parts.join('/'));
    }

    async function createFolder() {
        const folderName = prompt('Název nové složky:');
        if (!folderName || !folderName.trim()) {
            return;
        }

        await api('/api/filemanager/create-folder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path: state.currentPath, folderName: folderName.trim(), root: state.root || null })
        });

        await loadEntries(state.currentPath);
    }

    async function renameEntry(entry) {
        const newName = prompt('Nový název:', entry.name);
        if (!newName || !newName.trim() || newName === entry.name) {
            return;
        }

        await api('/api/filemanager/rename', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                path: entry.relativePath,
                newName: newName.trim(),
                isDirectory: !!entry.isDirectory,
                root: state.root || null
            })
        });

        await loadEntries(state.currentPath);
    }

    async function deleteEntry(entry) {
        if (!confirm(`Opravdu smazat ${entry.name}?`)) {
            return;
        }

        await api('/api/filemanager/delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path: entry.relativePath, isDirectory: !!entry.isDirectory, root: state.root || null })
        });

        await loadEntries(state.currentPath);
    }

    async function deleteMultipleEntries(entries) {
        if (!confirm(`Opravdu smazat ${entries.length} položek?`)) {
            return;
        }

        await api('/api/filemanager/delete-multiple', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                path: state.currentPath,
                fileNames: entries.filter((x) => !x.isDirectory).map((x) => x.name),
                directoryNames: entries.filter((x) => x.isDirectory).map((x) => x.name),
                root: state.root || null
            })
        });

        await loadEntries(state.currentPath);
    }

    async function zipEntry(entry) {
        const body = {
            path: state.currentPath,
            fileNames: entry.isDirectory ? [] : [entry.name],
            directoryNames: entry.isDirectory ? [entry.name] : [],
            root: state.root || null
        };

        await api('/api/filemanager/zip', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        await loadEntries(state.currentPath);
    }

    async function zipEntries(entries) {
        await api('/api/filemanager/zip', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                path: state.currentPath,
                fileNames: entries.filter((x) => !x.isDirectory).map((x) => x.name),
                directoryNames: entries.filter((x) => x.isDirectory).map((x) => x.name),
                root: state.root || null
            })
        });

        await loadEntries(state.currentPath);
    }

    async function unzipEntry(entry) {
        await api('/api/filemanager/unzip', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path: state.currentPath, zipFileName: entry.name, root: state.root || null })
        });

        await loadEntries(state.currentPath);
    }

    async function uploadFiles(files) {
        if (!files || files.length === 0) {
            return;
        }

        const formData = new FormData();
        for (const file of files) {
            formData.append('files', file);
        }

        const encodedPath = encodeURIComponent(encodeUrlParam(state.currentPath));
        await api(`/api/filemanager/upload?path=${encodedPath}${buildRootQuerySuffix()}`, {
            method: 'POST',
            body: formData
        });

        await loadEntries(state.currentPath);
    }

    function downloadEntry(entry) {
        if (!entry.url) {
            return;
        }

        window.open(entry.url, '_blank', 'noopener');
    }

    async function openEditor(entry) {
        if (!entry.url) {
            return;
        }

        const response = await fetch(entry.url, { cache: 'no-store' });
        if (!response.ok) {
            alert('Soubor se nepodařilo načíst.');
            return;
        }

        const content = await response.text();

        state.editor.filePath = state.currentPath;
        state.editor.fileName = entry.name;
        state.editor.isOpen = true;

        el.editorTitle.textContent = entry.name;
        el.editorStatus.textContent = '';
        el.editorModal.hidden = false;
        document.body.style.overflow = 'hidden';

        initializeEditorIfNeeded();

        state.editor.ace.setValue(content || '', -1);
        const language = detectLanguage(entry.name);
        el.editorLanguage.value = language;
        applyEditorLanguage(language);

        state.editor.ace.focus();
    }

    function initializeEditorIfNeeded() {
        if (state.editor.ace) {
            return;
        }

        ace.config.set('basePath', '/Lib/Filemanager/ace');

        state.editor.ace = ace.edit('rfm-editor');
        state.editor.ace.setTheme('ace/theme/chrome');
        state.editor.ace.setOption('useWorker', false);
        state.editor.ace.session.setUseWorker(false);
        state.editor.ace.session.setMode('ace/mode/text');
        state.editor.ace.setOptions({
            fontSize: '14px',
            tabSize: 4,
            useSoftTabs: true,
            showPrintMargin: false,
            wrap: false
        });
        state.editor.ace.session.setUseWorker(false);

        state.editor.ace.commands.addCommand({
            name: 'saveFile',
            bindKey: { win: 'Ctrl-S', mac: 'Command-S' },
            exec: async () => {
                await saveEditor();
            }
        });
    }

    async function saveEditor() {
        if (!state.editor.ace || !state.editor.isOpen) {
            return;
        }

        const cursor = state.editor.ace.getCursorPosition();
        const firstVisibleRow = state.editor.ace.getFirstVisibleRow();
        const content = state.editor.ace.getValue();

        el.editorSave.disabled = true;
        setEditorStatus('Ukládám...', false);

        try {
            await ajaxSaveText({
                path: state.editor.filePath,
                fileName: state.editor.fileName,
                content,
                root: state.root || null
            });

            state.editor.ace.focus();
            state.editor.ace.moveCursorToPosition(cursor);
            state.editor.ace.renderer.scrollToRow(firstVisibleRow);
            setEditorStatus('Uloženo', false);
        } catch (error) {
            setEditorStatus(error.message || 'Chyba ukládání.', true);
        } finally {
            el.editorSave.disabled = false;
        }
    }

    function closeEditor() {
        if (!state.editor.isOpen) {
            return;
        }

        state.editor.isOpen = false;
        el.editorModal.hidden = true;
        document.body.style.overflow = '';
    }

    function toggleEditorTheme() {
        if (!state.editor.ace) {
            return;
        }

        state.editor.isDark = !state.editor.isDark;

        if (state.editor.isDark) {
            state.editor.ace.setTheme('ace/theme/monokai');
            el.editorTheme.innerHTML = '<i class="bi bi-sun"></i>';
            el.editorWindow.classList.add('theme-dark');
        } else {
            state.editor.ace.setTheme('ace/theme/chrome');
            el.editorTheme.innerHTML = '<i class="bi bi-moon"></i>';
            el.editorWindow.classList.remove('theme-dark');
        }

        state.editor.ace.focus();
    }

    function applyEditorLanguage(language) {
        if (!state.editor.ace) {
            return;
        }

        const mode = toAceMode(language === 'auto' ? detectLanguage(state.editor.fileName) : language);
        state.editor.ace.setOption('useWorker', false);
        state.editor.ace.session.setUseWorker(false);
        state.editor.ace.session.setMode(mode);
        state.editor.ace.session.setUseWorker(false);
        state.editor.ace.focus();
    }

    function detectLanguage(fileName) {
        const ext = getExtension(fileName);
        switch (ext) {
            case 'html':
            case 'htm':
                return 'html';
            case 'css':
                return 'css';
            case 'js':
            case 'jsx':
                return 'javascript';
            case 'ts':
            case 'tsx':
                return 'typescript';
            case 'php':
                return 'php';
            case 'cs':
                return 'csharp';
            case 'c':
            case 'h':
            case 'cpp':
            case 'cc':
            case 'cxx':
            case 'hpp':
                return 'cpp';
            case 'java':
                return 'java';
            case 'py':
                return 'python';
            case 'rb':
                return 'ruby';
            case 'go':
                return 'go';
            case 'rs':
                return 'rust';
            case 'sql':
                return 'sql';
            case 'json':
                return 'json';
            case 'xml':
                return 'xml';
            case 'yaml':
            case 'yml':
                return 'yaml';
            case 'md':
                return 'markdown';
            default:
                return 'txt';
        }
    }

    function toAceMode(language) {
        switch ((language || '').toLowerCase()) {
            case 'html': return 'ace/mode/html';
            case 'css': return 'ace/mode/css';
            case 'javascript': return 'ace/mode/javascript';
            case 'typescript': return 'ace/mode/typescript';
            case 'php': return 'ace/mode/php';
            case 'csharp': return 'ace/mode/csharp';
            case 'cpp': return 'ace/mode/c_cpp';
            case 'java': return 'ace/mode/java';
            case 'python': return 'ace/mode/python';
            case 'ruby': return 'ace/mode/ruby';
            case 'go': return 'ace/mode/golang';
            case 'rust': return 'ace/mode/rust';
            case 'sql': return 'ace/mode/sql';
            case 'json': return 'ace/mode/json';
            case 'xml': return 'ace/mode/xml';
            case 'yaml': return 'ace/mode/yaml';
            case 'markdown': return 'ace/mode/markdown';
            default: return 'ace/mode/text';
        }
    }

    function setEditorStatus(message, isError) {
        el.editorStatus.textContent = message || '';
        el.editorStatus.style.color = isError ? '#b42318' : '#4b5563';
    }

    function ajaxSaveText(payload) {
        if (window.$ && $.ajax) {
            return new Promise((resolve, reject) => {
                $.ajax({
                    url: '/api/filemanager/save-text',
                    method: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify(payload),
                    success: resolve,
                    error: (xhr) => {
                        const msg = xhr?.responseJSON?.message || xhr?.statusText || 'Uložení selhalo.';
                        reject(new Error(msg));
                    }
                });
            });
        }

        // Fallback when jQuery is not available (still AJAX).
        return api('/api/filemanager/save-text', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
    }

    function resolveDropPathFromRow(row) {
        const rel = row.dataset.rel;
        if (!rel) {
            return null;
        }

        if (rel === '__parent__') {
            return getParentPath(state.currentPath);
        }

        const entry = state.entries.find((x) => x.relativePath === rel);
        if (!entry || !entry.isDirectory) {
            return null;
        }

        return entry.relativePath;
    }

    function initializePickerMode() {
        const params = new URLSearchParams(window.location.search);
        const pick = decodeUrlParam(params.get('picker') || params.get('select') || '').toLowerCase();
        const only = decodeUrlParam(params.get('onlyImages') || params.get('onlyIMG') || '').toLowerCase();
        const allowExtRaw = decodeUrlParam(params.get('allowExt') || params.get('extensions') || '');
        const rootRaw = decodeUrlParam(params.get('root') || params.get('setPath') || params.get('r') || '');

        state.picker.enabled = pick === '1' || pick === 'true' || pick === 'yes';
        state.picker.onlyImages = only === '1' || only === 'true' || only === 'yes';
        state.picker.allowedExtensions = parseAllowedExtensions(allowExtRaw);
        state.picker.callback = decodeUrlParam(params.get('callback') || params.get('f') || '') || 'ZachyceniURLCKeditor';
        state.picker.setPath = decodeUrlParam(params.get('setPath') || params.get('p') || '');
        state.root = normalizeRoot(rootRaw);
    }

    function canPickEntry(entry) {
        if (!entry || entry.isDirectory) {
            return false;
        }

        return isEntryAllowedByPicker(entry);
    }

    function isEntryAllowedByPicker(entry) {
        if (!entry || entry.isDirectory) {
            return false;
        }

        if (state.picker.allowedExtensions && state.picker.allowedExtensions.size > 0) {
            return state.picker.allowedExtensions.has(getExtension(entry.name));
        }

        if (state.picker.onlyImages) {
            return isImageFile(entry.name);
        }

        return true;
    }

    function parseAllowedExtensions(raw) {
        if (!raw) {
            return null;
        }

        const items = String(raw)
            .split(',')
            .map((x) => x.trim().toLowerCase().replace(/^\./, ''))
            .filter((x) => x.length > 0);

        if (!items.length) {
            return null;
        }

        return new Set(items);
    }

    function selectEntryForPicker(entry) {
        if (!state.picker.enabled || !canPickEntry(entry)) {
            return;
        }

        const targetWindow = window.opener;
        if (!targetWindow || targetWindow.closed) {
            alert('Rodicovske okno editoru neni dostupne.');
            return;
        }

        const imageUrl = entry.url ? new URL(entry.url, window.location.origin).href : '';
        if (!imageUrl) {
            alert('Nepodarilo se ziskat URL souboru.');
            return;
        }

        const callbackName = state.picker.callback || 'ZachyceniURLCKeditor';
        const callback = targetWindow[callbackName];
        if (typeof callback !== 'function') {
            alert(`Funkce ${callbackName} nebyla v editoru nalezena.`);
            return;
        }

        callback(imageUrl, entry.name);
        window.close();
    }

    function setDropTargetIndicator(element) {
        if (state.drag.dropTargetElement === element) {
            return;
        }

        clearDropTargetIndicator();
        state.drag.dropTargetElement = element;
        state.drag.dropTargetElement.classList.add('drop-target');
    }

    function clearDropTargetIndicator() {
        if (state.drag.dropTargetElement) {
            state.drag.dropTargetElement.classList.remove('drop-target');
            state.drag.dropTargetElement = null;
        }
    }

    function resetDragState() {
        clearDropTargetIndicator();
        for (const row of el.body.querySelectorAll('tr.drag-source')) {
            row.classList.remove('drag-source');
        }
        for (const tile of el.tiles.querySelectorAll('.rfm-tile.drag-source')) {
            tile.classList.remove('drag-source');
        }

        for (const item of el.tree.querySelectorAll('.fm-tree-item.drag-target')) {
            item.classList.remove('drag-target');
        }

        state.drag.active = false;
        state.drag.payload = null;
    }

    async function performDrop(targetPath, copyMode) {
        const payload = state.drag.payload;
        if (!payload) {
            resetDragState();
            return;
        }

        const normalizedTarget = normalizeRelative(targetPath || '');
        const normalizedSource = normalizeRelative(payload.sourcePath || '');

        if (!copyMode && normalizedTarget === normalizedSource) {
            resetDragState();
            return;
        }

        if (!copyMode) {
            for (const dir of payload.entries.filter((x) => x.isDirectory)) {
                const sourceDirPath = normalizeRelative(dir.relativePath);
                if (normalizedTarget === sourceDirPath || normalizedTarget.startsWith(`${sourceDirPath}/`)) {
                    alert('Nelze přesunout složku do sebe sama.');
                    resetDragState();
                    return;
                }
            }
        }

        await api('/api/filemanager/copy', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sourcePath: payload.sourcePath,
                targetPath: normalizedTarget,
                fileNames: payload.fileNames,
                directoryNames: payload.directoryNames,
                move: !copyMode,
                root: state.root || null
            })
        });

        resetDragState();
        await loadEntries(state.currentPath);
    }

    function updateSelection(relativePath, index, additive, range) {
        if (range && state.lastSelectedIndex >= 0) {
            if (!additive) {
                state.selectedPaths.clear();
            }

            const [from, to] = state.lastSelectedIndex <= index
                ? [state.lastSelectedIndex, index]
                : [index, state.lastSelectedIndex];

            for (let i = from; i <= to; i++) {
                const rel = state.visibleEntries[i]?.relativePath;
                if (rel) {
                    state.selectedPaths.add(rel);
                }
            }

            state.selectedPath = relativePath;
            applySelectionToRows();
            return;
        }

        if (additive) {
            if (state.selectedPaths.has(relativePath)) {
                state.selectedPaths.delete(relativePath);
            } else {
                state.selectedPaths.add(relativePath);
            }

            state.selectedPath = relativePath;
            state.lastSelectedIndex = index;
            applySelectionToRows();
            return;
        }

        selectSingle(relativePath, index);
    }

    function selectSingle(relativePath, index) {
        state.selectedPaths.clear();
        if (relativePath) {
            state.selectedPaths.add(relativePath);
            state.selectedPath = relativePath;
            state.lastSelectedIndex = index;
        } else {
            state.selectedPath = null;
            state.lastSelectedIndex = -1;
        }

        applySelectionToRows();
    }

    function clearSelection() {
        state.selectedPaths.clear();
        state.selectedPath = null;
        state.lastSelectedIndex = -1;
        applySelectionToRows();
    }

    function selectAllVisible() {
        state.selectedPaths.clear();
        for (const entry of state.visibleEntries) {
            state.selectedPaths.add(entry.relativePath);
        }

        state.lastSelectedIndex = state.visibleEntries.length - 1;
        state.selectedPath = state.visibleEntries[state.lastSelectedIndex]?.relativePath || null;
        applySelectionToRows();
    }

    function applySelectionToRows() {
        for (const row of el.body.querySelectorAll('tr[data-rel]')) {
            const rel = row.dataset.rel;
            row.classList.toggle('selected', !!rel && state.selectedPaths.has(rel));
        }
        for (const tile of el.tiles.querySelectorAll('.rfm-tile[data-rel]')) {
            const rel = tile.dataset.rel;
            tile.classList.toggle('selected', !!rel && state.selectedPaths.has(rel));
        }
    }

    function getSelectedEntries() {
        if (!state.selectedPaths.size) {
            return [];
        }

        return state.entries.filter((x) => state.selectedPaths.has(x.relativePath));
    }

    function resolveActionEntries(target, allowMultiple = false) {
        const selected = getSelectedEntries();
        if (allowMultiple && selected.length > 1) {
            return selected;
        }

        if (target) {
            return [target];
        }

        if (selected.length > 0) {
            return allowMultiple ? selected : [selected[0]];
        }

        return [];
    }

    function getFileVisualHtml(entry) {
        if (isImageFile(entry.name)) {
            const previewUrl = buildPreviewUrl(entry);
            return `<span class="rfm-file-visual"><img class="rfm-thumb" src="${escapeHtml(previewUrl)}" alt="${escapeHtml(entry.name)}" loading="lazy" onerror="this.style.display='none'; this.nextElementSibling.style.display='inline-flex';" /><span class="rfm-icon-chip rfm-fallback" style="display:none;"><i class="bi ${getFileIconGlyph(entry.name)}"></i></span></span>`;
        }

        return `<span class="rfm-icon-chip"><i class="bi ${getFileIconGlyph(entry.name)}"></i></span>`;
    }

    function getFileIconGlyph(name) {
        const ext = getExtension(name);

        if (['js', 'jsx', 'ts', 'tsx', 'json'].includes(ext)) return 'bi-braces-asterisk';
        if (['cs', 'java', 'cpp', 'cc', 'cxx', 'h', 'hpp', 'py', 'rb', 'go', 'rs', 'php'].includes(ext)) return 'bi-file-code';
        if (['html', 'htm', 'xml'].includes(ext)) return 'bi-filetype-html';
        if (['css', 'scss', 'sass', 'less'].includes(ext)) return 'bi-palette2';
        if (['sql'].includes(ext)) return 'bi-database';
        if (['md', 'txt', 'log'].includes(ext)) return 'bi-file-text';
        if (['zip', 'rar', '7z'].includes(ext)) return 'bi-file-zip';
        if (['pdf'].includes(ext)) return 'bi-filetype-pdf';
        if (['doc', 'docx'].includes(ext)) return 'bi-file-earmark-word';
        if (['xls', 'xlsx', 'csv'].includes(ext)) return 'bi-file-earmark-spreadsheet';
        if (['ppt', 'pptx'].includes(ext)) return 'bi-file-earmark-slides';
        if (['mp3', 'wav', 'ogg', 'flac'].includes(ext)) return 'bi-file-earmark-music';
        if (['mp4', 'mkv', 'avi', 'mov', 'wmv'].includes(ext)) return 'bi-file-earmark-play';
        return 'bi-file-earmark';
    }

    function buildPreviewUrl(entry) {
        const ext = getExtension(entry.name);
        if (ext === 'svg' && entry.url) {
            return entry.url;
        }

        const split = splitRelative(entry.relativePath);
        const encodedPath = encodeURIComponent(encodeUrlParam(split.path));
        const encodedFileName = encodeURIComponent(encodeUrlParam(split.name));
        return `/api/filemanager/thumbnail?path=${encodedPath}&fileName=${encodedFileName}${buildRootQuerySuffix()}`;
    }

    function buildRootQuerySuffix() {
        return state.root ? `&root=${encodeURIComponent(encodeUrlParam(state.root))}` : '';
    }

    function normalizeRoot(rawRoot) {
        const value = decodeUrlParam(rawRoot || '');
        return value.replaceAll('\\', '/').replace(/^\/+|\/+$/g, '');
    }

    function encodeUrlParam(value) {
        const input = value == null ? '' : String(value);
        return `b64:${toBase64UrlUtf8(input)}`;
    }

    function decodeUrlParam(rawValue) {
        if (rawValue == null) {
            return '';
        }

        const value = String(rawValue).trim();
        if (!value) {
            return '';
        }

        if (value.startsWith('b64:')) {
            const decoded = fromBase64UrlUtf8(value.substring(4));
            return decoded ?? '';
        }

        // Backward compatibility for historical plain Base64 values.
        const legacy = tryDecodeLegacyBase64(value);
        return legacy ?? value;
    }

    function tryDecodeLegacyBase64(value) {
        if (!value || value.length < 8 || value.length % 4 !== 0 || !/^[A-Za-z0-9+/]+={0,2}$/.test(value)) {
            return null;
        }

        try {
            const decoded = atob(value);
            if (!decoded || !decoded.trim()) {
                return null;
            }

            if (!/^[\w\s./\-\\\u00C0-\u024F]+$/.test(decoded)) {
                return null;
            }

            return decoded;
        } catch (e) {
            return null;
        }
    }

    function toBase64UrlUtf8(value) {
        const bytes = new TextEncoder().encode(value);
        let binary = '';
        for (let i = 0; i < bytes.length; i++) {
            binary += String.fromCharCode(bytes[i]);
        }

        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    }

    function fromBase64UrlUtf8(value) {
        if (!value) {
            return '';
        }

        try {
            const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = normalized.length % 4 === 0 ? 0 : 4 - (normalized.length % 4);
            const padded = normalized + '='.repeat(padLength);
            const binary = atob(padded);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                bytes[i] = binary.charCodeAt(i);
            }
            return new TextDecoder().decode(bytes);
        } catch (e) {
            return null;
        }
    }

    function getRootLabel() {
        const normalized = normalizeRoot(state.root);
        if (!normalized) {
            return 'UzivatelskeSoubory';
        }

        return normalized;
    }

    window.rfmSetRoot = async function (root) {
        state.root = normalizeRoot(root);
        await loadEntries('');
    };

    function splitRelative(relativePath) {
        const normalized = normalizeRelative(relativePath || '');
        const idx = normalized.lastIndexOf('/');
        if (idx < 0) {
            return { path: '', name: normalized };
        }

        return {
            path: normalized.substring(0, idx),
            name: normalized.substring(idx + 1)
        };
    }

    function getParentPath(path) {
        const normalized = normalizeRelative(path || '');
        if (!normalized) {
            return '';
        }

        const parts = normalized.split('/').filter(Boolean);
        parts.pop();
        return parts.join('/');
    }

    function normalizeRelative(path) {
        return String(path || '').replaceAll('\\', '/').replace(/^\/+|\/+$/g, '');
    }

    function isInputLike(target) {
        if (!target) {
            return false;
        }

        const tag = (target.tagName || '').toLowerCase();
        return tag === 'input' || tag === 'textarea' || tag === 'select' || target.isContentEditable;
    }

    function getExtension(name) {
        const i = name.lastIndexOf('.');
        if (i < 0 || i === name.length - 1) {
            return '';
        }
        return name.substring(i + 1).toLowerCase();
    }

    function isTextFile(name) {
        return TEXT_EXTENSIONS.has(getExtension(name));
    }

    function isImageFile(name) {
        return IMAGE_EXTENSIONS.has(getExtension(name));
    }

    function isZipFile(name) {
        return getExtension(name) === 'zip';
    }

    function formatSize(bytes) {
        if (!bytes || bytes < 1024) return `${bytes || 0} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
        return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
    }

    function formatDate(value) {
        if (!value) return '-';
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return '-';
        return d.toLocaleString('cs-CZ', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    document.addEventListener('DOMContentLoaded', initialize);
})();


