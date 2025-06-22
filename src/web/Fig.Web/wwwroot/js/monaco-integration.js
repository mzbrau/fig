window.monacoIntegration = {
    editors: new Map(),
    
    async initialize(elementId, options) {
        return new Promise((resolve) => {
            // Ensure require is available
            if (typeof require === 'undefined') {
                console.error('require.js is not loaded');
                resolve(null);
                return;
            }
            
            // Configure Monaco Editor paths to use CDN
            require.config({ 
                paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.44.0/min/vs' }
            });
            
            require(['vs/editor/editor.main'], () => {
                const element = document.getElementById(elementId);
                if (!element) {
                    console.error('Element not found:', elementId);
                    resolve(null);
                    return;
                }
                
                // Ensure monaco is available
                if (typeof monaco === 'undefined') {
                    console.error('Monaco Editor is not loaded');
                    resolve(null);
                    return;
                }
                
                try {
                    const editor = monaco.editor.create(element, {
                        value: options.value || '',
                        language: options.language || 'json',
                        theme: options.theme || 'vs-dark',
                        readOnly: options.readOnly || false,
                        automaticLayout: true,
                        scrollBeyondLastLine: false,
                        wordWrap: 'on',
                        tabSize: 2,
                        insertSpaces: true,
                        fontSize: 14,
                        lineNumbers: 'on',
                        renderWhitespace: 'boundary',
                        minimap: { enabled: false }
                    });
                    
                    this.editors.set(elementId, editor);
                    
                    // Set up JSON schema validation if provided
                    if (options.jsonSchema && options.language === 'json') {
                        this.setJsonSchema(elementId, options.jsonSchema);
                    }
                    
                    resolve(editor);
                } catch (error) {
                    console.error('Failed to create Monaco Editor:', error);
                    resolve(null);
                }
            });
        });
    },
    
    getValue(elementId) {
        const editor = this.editors.get(elementId);
        return editor ? editor.getValue() : '';
    },
    
    setValue(elementId, value) {
        const editor = this.editors.get(elementId);
        if (editor) {
            editor.setValue(value || '');
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
    
    onDidChangeModelContent(elementId, callback) {
        const editor = this.editors.get(elementId);
        if (editor) {
            return editor.onDidChangeModelContent(callback);
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
    }
};
