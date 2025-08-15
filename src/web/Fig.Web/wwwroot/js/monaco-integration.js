window.monacoIntegration = {
    editors: new Map(),
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
            
            console.log('Creating Monaco editor for element:', elementId);

            // Determine if this is a small editor (collapsed view) vs dialog editor
            const isSmallEditor = !elementId.includes('dialog');
            
            const editorOptions = {
                value: options.value || '',
                language: options.language || 'json',
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
                colorDecorators: true
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
            
            // Force layout and focus immediately after creation
            setTimeout(() => {
                editor.layout();
                // Ensure cursor is visible by setting position and focusing
                if (!options.readOnly) {
                    editor.setPosition({ lineNumber: 1, column: 1 });
                    if (isSmallEditor) {
                        // For small editors, force reveal position to ensure cursor visibility
                        editor.revealPosition({ lineNumber: 1, column: 1 });
                    }
                    editor.focus();
                }
            }, 100);
            
            // Add a listener to ensure cursor visibility when content changes
            editor.onDidChangeModelContent(() => {
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
            editor.onMouseDown(() => {
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
            const isSmallEditor = !elementId.includes('dialog');
            editor.setValue(value || '');
            // If setting empty value, ensure cursor is visible
            if (!value || value === '') {
                setTimeout(() => {
                    const position = { lineNumber: 1, column: 1 };
                    editor.setPosition(position);
                    if (isSmallEditor) {
                        editor.revealPosition(position);
                    }
                    editor.focus();
                }, 10);
            }
        }
    },
    
    setJsonSchema(elementId, schema) {
        try {
            const schemaObj = typeof schema === 'string' ? JSON.parse(schema) : schema;
            monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
                validate: true,
                schemas: [{
                    uri: `json-schema-${elementId}`,
                    fileMatch: ['*'],
                    schema: schemaObj
                }]
            });
        } catch (error) {
            console.error('Error setting JSON schema:', error);
        }
    },
    
    onDidChangeModelContent(elementId, dotNetObjectReference, methodName) {
        const editor = this.editors.get(elementId);
        if (editor && dotNetObjectReference && methodName) {
            return editor.onDidChangeModelContent(() => {
                try {
                    dotNetObjectReference.invokeMethodAsync(methodName);
                } catch (error) {
                    console.error('Error invoking .NET method:', error);
                }
            });
        }
        return null;
    },
    
    formatDocument(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.getAction('editor.action.formatDocument').run();
        }
    },
    
    dispose(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.dispose();
            this.editors.delete(elementId);
        }
    },
    
    resize(elementId) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.layout();
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
    }
};

// Create PascalCase alias for C# compatibility
window.MonacoIntegration = window.monacoIntegration;
