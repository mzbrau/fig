window.monacoIntegration = {
    editors: new Map(),
    editorMetadata: new Map(), // Store metadata like isDialog flag
    editorDisposables: new Map(), // Store disposables for cleanup
    schemas: new Map(), // Store JSON schemas by elementId
    isMonacoLoaded: false,
    loadingPromise: null,
    
    async loadMonaco() {
        if (this.isMonacoLoaded) {
            return Promise.resolve();
        }
        
        if (this.loadingPromise) {
            return this.loadingPromise;
        }
        
        this.loadingPromise = new Promise((resolve, reject) => {
            // Load CSS first
            if (!document.querySelector('link[href*="monaco-editor"]')) {
                const cssLink = document.createElement('link');
                cssLink.rel = 'stylesheet';
                cssLink.href = '/lib/monaco-editor/vs/editor/editor.main.css';
                document.head.appendChild(cssLink);
            }
            
            // Configure Monaco Environment for local workers
            window.MonacoEnvironment = {
                getWorkerUrl: function (moduleId, label) {
                    const getWorkerScript = (workerPath) => {
                        const loaderPath = '/lib/monaco-editor/vs/base/worker/workerMain.js';
                        const fullWorkerPath = '/lib/monaco-editor/vs' + workerPath;
                        
                        // We need to load the loader (workerMain.js) first, then the worker
                        // Using a data URI to create a worker that imports both
                        const script = `
                            self.MonacoEnvironment = {
                                baseUrl: '${window.location.origin}/lib/monaco-editor/'
                            };
                            importScripts('${window.location.origin}${loaderPath}');
                            importScripts('${window.location.origin}${fullWorkerPath}');
                        `;
                        return `data:text/javascript;charset=utf-8,${encodeURIComponent(script)}`;
                    };

                    // Route to the appropriate worker based on the language
                    if (label === 'json') {
                        return getWorkerScript('/language/json/jsonWorker.js');
                    }
                    if (label === 'css' || label === 'scss' || label === 'less') {
                        return getWorkerScript('/language/css/cssWorker.js');
                    }
                    if (label === 'html' || label === 'handlebars' || label === 'razor') {
                        return getWorkerScript('/language/html/htmlWorker.js');
                    }
                    if (label === 'typescript' || label === 'javascript') {
                        return getWorkerScript('/language/typescript/tsWorker.js');
                    }
                    
                    // Default to base worker for editor features
                    return '/lib/monaco-editor/vs/base/worker/workerMain.js';
                }
            };
            
            // Create script element to load Monaco's loader if not already loaded
            if (typeof require === 'undefined') {
                const loaderScript = document.createElement('script');
                loaderScript.src = '/lib/monaco-editor/vs/loader.js';
                loaderScript.onload = () => {
                    this.configureAndLoadMonaco(resolve, reject);
                };
                loaderScript.onerror = () => reject(new Error('Failed to load Monaco loader'));
                document.head.appendChild(loaderScript);
            } else {
                this.configureAndLoadMonaco(resolve, reject);
            }
        });
        
        return this.loadingPromise;
    },
    
    configureAndLoadMonaco(resolve, reject) {
        // Configure Monaco Editor paths to use local files
        require.config({ 
            paths: { 'vs': '/lib/monaco-editor/vs' }
        });
        
        require(['vs/editor/editor.main'], () => {
            this.isMonacoLoaded = true;
            console.log('Monaco Editor loaded successfully from local files');
            resolve();
        }, (error) => {
            reject(error);
        });
    },

    getEditor(elementId) {
        return this.editors.get(elementId);
    },
    
    /**
     * Initialize a Monaco editor for the specified element.
     * 
     * If an editor already exists for this elementId, it will be fully disposed
     * (including event listeners, model, and schema) before creating a new one.
     * This ensures clean state without stale listeners or configuration mismatches.
     * 
     * @param {string} elementId - The DOM element ID to attach the editor to
     * @param {Object} options - Editor configuration options
     * @param {string} [options.value] - Initial editor content
     * @param {string} [options.language='json'] - Editor language mode
     * @param {string} [options.theme='vs-dark'] - Editor theme
     * @param {boolean} [options.readOnly=false] - Whether the editor is read-only
     * @param {boolean} [options.automaticLayout=true] - Whether to auto-resize
     * @param {boolean} [options.isDialog] - Whether this is a dialog editor (affects UI)
     * @param {Object} [options.jsonSchema] - JSON schema for validation
     * @returns {Promise<monaco.editor.IStandaloneCodeEditor|null>} The created editor or null on failure
     */
    async initialize(elementId, options) {
        try {
            // Load Monaco if not already loaded
            await this.loadMonaco();
            
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('Element not found:', elementId);
                return null;
            }
            
            // Ensure monaco is available
            if (typeof monaco === 'undefined') {
                console.error('Monaco Editor is not loaded');
                return null;
            }
            
            // If an editor already exists for this elementId, fully dispose it first
            // to prevent stale state, lingering listeners, and config mismatches
            if (this.editors.has(elementId)) {
                console.log('Disposing existing editor before re-initialization:', elementId);
                this.dispose(elementId);
            }
            
            console.log('Creating Monaco editor for element:', elementId);

            // Determine if this is a small editor (collapsed view) vs dialog editor
            // Use explicit isDialog option or fallback to element ID check for backward compatibility
            const isDialog = options.isDialog !== undefined ? options.isDialog : elementId.includes('dialog');
            const isSmallEditor = !isDialog;
            
            // Create model with specific URI to support schema validation isolation
            // Use a unique URI incorporating a timestamp to ensure fresh model state
            // even if a stale model with the same URI somehow persists
            const timestamp = Date.now();
            const modelUri = monaco.Uri.parse(`inmemory://model/${elementId}-${timestamp}.json`);
            
            // Double-check: dispose any orphaned model with this URI (should not happen
            // after dispose() but guards against edge cases)
            let existingModel = monaco.editor.getModel(modelUri);
            if (existingModel) {
                console.warn('Found orphaned model, disposing:', modelUri.toString());
                existingModel.dispose();
            }
            
            const model = monaco.editor.createModel(options.value || '', options.language || 'json', modelUri);
            
            const editorOptions = {
                model: model,
                theme: options.theme || 'vs-dark',
                readOnly: options.readOnly || false,
                automaticLayout: options.automaticLayout !== false,
                scrollBeyondLastLine: false,
                wordWrap: 'on',
                tabSize: 2,
                insertSpaces: true,
                fontSize: 14,
                lineNumbers: 'on',
                renderWhitespace: 'boundary',
                minimap: { enabled: !isSmallEditor }, // Disable minimap for small editors
                cursorBlinking: 'blink',
                cursorStyle: 'line',
                cursorWidth: 2,
                selectOnLineNumbers: true,
                mouseWheelZoom: false,
                contextmenu: true,
                smoothScrolling: true,
                overviewRulerBorder: false,
                hideCursorInOverviewRuler: true,
                overviewRulerLanes: 0,
                renderLineHighlight: 'all',
                renderControlCharacters: true,
                links: true,
                colorDecorators: true,
                // Fix context menu positioning in dialogs
                fixedOverflowWidgets: true
            };
            
            // Additional configuration for small editors to fix cursor visibility
            if (isSmallEditor) {
                editorOptions.lineNumbersMinChars = 3;
                editorOptions.glyphMargin = false;
                editorOptions.folding = false;
                editorOptions.lineDecorationsWidth = 5;
                editorOptions.lineNumbers = 'on';
                editorOptions.renderLineHighlight = 'gutter';
            }

            const editor = monaco.editor.create(element, editorOptions);
            
            // Force layout immediately after creation
            setTimeout(() => {
                editor.layout();
                // Ensure cursor is at the start
                if (!options.readOnly) {
                    editor.setPosition({ lineNumber: 1, column: 1 });
                    if (isSmallEditor) {
                        // For small editors, force reveal position to ensure cursor visibility
                        editor.revealPosition({ lineNumber: 1, column: 1 });
                    }
                }
            }, 100);
            
            // Add a listener to ensure cursor visibility when content changes
            const contentChangeDisposable = editor.onDidChangeModelContent(() => {
                setTimeout(() => {
                    if (!options.readOnly && editor.getValue() === '') {
                        editor.setPosition({ lineNumber: 1, column: 1 });
                        if (isSmallEditor) {
                            editor.revealPosition({ lineNumber: 1, column: 1 });
                        }
                    }
                }, 10);
            });
            
            // Add click listener to ensure focus and cursor visibility
            const mouseDownDisposable = editor.onMouseDown(() => {
                if (!options.readOnly) {
                    setTimeout(() => {
                        editor.focus();
                        const position = editor.getPosition();
                        if (position && isSmallEditor) {
                            // Force cursor visibility for small editors
                            editor.revealPosition(position);
                        }
                        if (editor.getValue() === '') {
                            editor.setPosition({ lineNumber: 1, column: 1 });
                            if (isSmallEditor) {
                                editor.revealPosition({ lineNumber: 1, column: 1 });
                            }
                        }
                    }, 10);
                }
            });
            
            this.editors.set(elementId, editor);
            this.editorMetadata.set(elementId, { isDialog, modelUri: modelUri.toString() });
            
            // Store initial disposables for cleanup
            this.editorDisposables.set(elementId, [contentChangeDisposable, mouseDownDisposable]);
            
            // Set up JSON schema validation if provided
            if (options.jsonSchema && options.language === 'json') {
                this.setJsonSchema(elementId, options.jsonSchema);
            }
            
            console.log('Monaco editor created successfully for:', elementId);
            return editor;
        } catch (error) {
            console.error('Failed to create Monaco Editor:', error);
            return null;
        }
    },
    
    getValue(elementId) {
        const editor = this.editors.get(elementId);
        return editor ? editor.getValue() : '';
    },
    
    setValue(elementId, value) {
        const editor = this.editors.get(elementId);
        if (editor) {
            const metadata = this.editorMetadata.get(elementId);
            const isDialog = metadata ? metadata.isDialog : elementId.includes('dialog'); // Fallback for backward compatibility
            const isSmallEditor = !isDialog;
            editor.setValue(value || '');
            // If setting empty value, ensure cursor is at start
            if (!value || value === '') {
                setTimeout(() => {
                    const position = { lineNumber: 1, column: 1 };
                    editor.setPosition(position);
                    if (isSmallEditor) {
                        editor.revealPosition(position);
                    }
                }, 10);
            }
        }
    },
    
    /**
     * Update Monaco's JSON diagnostics with all registered schemas.
     * Uses the actual model URIs stored in metadata to ensure correct matching
     * with timestamp-based model URIs.
     */
    updateJsonDiagnostics() {
        const schemas = [];
        for (const [elementId, schemaData] of this.schemas) {
            const metadata = this.editorMetadata.get(elementId);
            if (metadata && metadata.modelUri) {
                schemas.push({
                    uri: `inmemory://schema/${elementId}`,
                    fileMatch: [metadata.modelUri],
                    schema: schemaData
                });
            }
        }
        
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: schemas
        });
    },

    /**
     * Set a JSON schema for validation on a specific editor.
     * The editor must already be initialized via initialize() before calling this.
     * 
     * @param {string} elementId - The element ID of the editor
     * @param {Object|string} schema - The JSON schema object or JSON string
     */
    setJsonSchema(elementId, schema) {
        try {
            // Verify editor exists - schema is only valid if editor is initialized
            if (!this.editors.has(elementId)) {
                console.warn('Cannot set schema for non-existent editor:', elementId);
                return;
            }
            
            const schemaObj = typeof schema === 'string' ? JSON.parse(schema) : schema;
            this.schemas.set(elementId, schemaObj);
            this.updateJsonDiagnostics();
        } catch (error) {
            console.error('Error setting JSON schema:', error);
        }
    },
    
    onDidChangeModelContent(elementId, dotNetObjectReference, methodName) {
        const editor = this.editors.get(elementId);
        if (editor && dotNetObjectReference && methodName) {
            const disposable = editor.onDidChangeModelContent(() => {
                try {
                    dotNetObjectReference.invokeMethodAsync(methodName);
                } catch (error) {
                    console.error('Error invoking .NET method:', error);
                }
            });
            
            // Store disposable for cleanup
            if (!this.editorDisposables.has(elementId)) {
                this.editorDisposables.set(elementId, []);
            }
            this.editorDisposables.get(elementId).push(disposable);
            
            return disposable;
        }
        return null;
    },
    
    formatDocument(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.getAction('editor.action.formatDocument').run();
        }
    },
    
    /**
     * Dispose an editor and clean up all associated resources.
     * 
     * This method performs a complete teardown including:
     * - Disposing all registered event listeners/disposables
     * - Removing the JSON schema (if any)
     * - Disposing the Monaco model
     * - Disposing the Monaco editor instance
     * - Clearing all metadata
     * 
     * After calling dispose(), initialize() can be safely called again
     * for the same elementId to create a fresh editor.
     * 
     * @param {string} elementId - The element ID of the editor to dispose
     */
    dispose(elementId) {
        // Remove schema if exists
        if (this.schemas.has(elementId)) {
            this.schemas.delete(elementId);
            this.updateJsonDiagnostics();
        }

        const editor = this.editors.get(elementId);
        if (editor) {
            // Dispose all event listeners for this editor
            const disposables = this.editorDisposables.get(elementId);
            if (disposables) {
                disposables.forEach(disposable => {
                    try {
                        disposable.dispose();
                    } catch (error) {
                        console.error('Error disposing event listener:', error);
                    }
                });
                this.editorDisposables.delete(elementId);
            }
            
            // Dispose model since we created it explicitly
            const model = editor.getModel();
            
            editor.dispose();
            
            if (model) {
                model.dispose();
            }

            this.editors.delete(elementId);
            this.editorMetadata.delete(elementId); // Clean up metadata
        }
    },
    
    resize(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.layout();
        }
    },
    
    setReadOnly(elementId, readOnly) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.updateOptions({ readOnly: readOnly });
            console.log(`Set editor ${elementId} readOnly to: ${readOnly}`);
            
            // If switching to editable mode, focus and position cursor
            if (!readOnly) {
                setTimeout(() => {
                    editor.focus();
                    const position = editor.getPosition() || { lineNumber: 1, column: 1 };
                    editor.setPosition(position);
                    editor.revealPosition(position);
                }, 50);
            }
        }
    },
    
    focus(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.focus();
            // Ensure cursor is at position 1,1 if content is empty
            if (editor.getValue() === '') {
                editor.setPosition({ lineNumber: 1, column: 1 });
            }
        }
    },
    
    // Dispose all editors and clean up all resources
    disposeAll() {
        // Dispose all editors
        for (const elementId of this.editors.keys()) {
            this.dispose(elementId);
        }
        
        // Clear all maps
        this.editors.clear();
        this.editorMetadata.clear();
        this.editorDisposables.clear();
    }
};

// Create PascalCase alias for C# compatibility
window.MonacoIntegration = window.monacoIntegration;
