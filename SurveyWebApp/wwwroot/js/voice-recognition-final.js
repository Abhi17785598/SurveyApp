let FLOW_STEP = 0;
let QUESTION_INDEX = 0;

// CRITICAL: Use window scope for flags to prevent glitches
window.isSpeaking = false;
window.questionRetryCount = 0;
window.flowRetryCount = 0;

console.log('🔄 Voice Recognition v16.2 - EVENT-DRIVEN ANDROID BRIDGES -', new Date().toISOString());

if (window.VOICE_RECOGNITION_VERSION) {
    console.log('🔄 Clearing previous version:', window.VOICE_RECOGNITION_VERSION);
}
window.VOICE_RECOGNITION_VERSION = '16.2';

// 🎤 SPEECH RECOGNITION CALLBACKS (NEW EVENT-DRIVEN)
window.onSpeechReady = function() {
    console.log('🎤 Speech recognition ready');
    if (window.voiceService) {
        window.voiceService.isListening = true;
        window.voiceService.updateStatus('Listening...');
    }
};

window.onSpeechEnd = function(text) {
    console.log('🎤 Speech recognition ended:', text);
    if (window.voiceService) {
        window.voiceService.isListening = false;
        window.voiceService.updateStatus('Processing...');
        
        // Process the recognized text
        if (text && text.trim()) {
            handleVoiceInput(text.trim());
        } else {
            window.voiceService.updateStatus('No speech detected');
            setTimeout(() => {
                if (FLOW_STEP < 3) {
                    startVoiceFlow();
                }
            }, 2000);
        }
    }
};

window.onSpeechError = function(error) {
    console.log('🎤 Speech recognition error:', error);
    if (window.voiceService) {
        window.voiceService.isListening = false;
        window.voiceService.updateStatus('Error: ' + error);
        
        // Retry after error
        setTimeout(() => {
            if (FLOW_STEP < 3) {
                startVoiceFlow();
            }
        }, 3000);
    }
};

window.onAndroidBridgeReady = function() {
    console.log('🚀 Android bridges ready - initializing app...');
    
    // Mark that Android bridge ready was called
    window.androidBridgeReadyCalled = true;
    
    // Initialize voice service and location handler
    window.voiceService = new VoiceRecognitionService();
    window.locationHandler = new LocationHandler();
    
    // Setup UI components
    window.voiceService.setupButtons();
    window.voiceService.setupEventListeners();
    
    // Start initial voice flow
    setTimeout(() => {
        if (FLOW_STEP === 0) {
            console.log('🔧 Starting initial voice flow');
            startVoiceFlow();
        }
    }, 1000);
    
    // Automatic location capture on app load
    console.log('📍 Capturing LOGIN location...');
    if (window.AndroidLocationBridge) {
        window.AndroidLocationBridge.captureLocationForEvent('LOGIN');
        console.log('📍 LOGIN location capture initiated');
    }
    
    // Test server connection
    console.log('🌐 Testing server connection...');
    if (window.AndroidIPConfig) {
        window.testServerConnection();
    }
};

window.onSpeechReady = function() {
    console.log('✅ Speech Recognition Ready - Microphone Active');
    window.isListening = true;
    window.voiceService?.updateStatus('Listening...');
    window.voiceService?.updateButtonState(true);
};

window.onSpeechEnd = function(text) {
    console.log('🛑 Speech Recognition Ended - Microphone Stopped');
    console.log('🎤 Recognized text:', text);
    window.isListening = false;
    window.voiceService?.updateStatus('Processing...');
    window.voiceService?.updateButtonState(false);
    
    // Process the recognized text
    if (text && text.trim()) {
        handleVoiceInput(text.trim());
    } else {
        console.log('❌ No speech detected, retrying...');
        setTimeout(() => {
            if (FLOW_STEP < 3) {
                startVoiceFlow();
            }
        }, 2000);
    }
};

window.onSpeechError = function(error) {
    console.error('❌ Speech Recognition Error:', error);
    window.isListening = false;
    window.voiceService?.updateStatus('Error: ' + error);
    window.voiceService?.updateButtonState(false);
    window.voiceService?.onNativeSpeechError(error);
};

// 🌐 IP CONNECTION CALLBACK (UNCHANGED - ALREADY ASYNCHRONOUS)
window.onTestConnectionResult = function(result) {
    console.log('🌐 Connection test result received:', result);
    
    const statusElement = document.getElementById('connection-status');
    if (statusElement) {
        if (result.success) {
            statusElement.textContent = '✅ Connected';
            statusElement.style.color = 'green';
        } else {
            statusElement.textContent = '❌ Connection Failed: ' + result.message;
            statusElement.style.color = 'red';
        }
    }
    
    if (result.success) {
        console.log('🌐 Server connection successful');
        window.fromNativeSpeech?.('Server connection successful');
    } else {
        console.error('🌐 Server connection failed:', result.message);
        window.fromNativeSpeech?.('Server connection failed: ' + result.message);
    }
};

// 🌐 TEST SERVER CONNECTION
window.testServerConnection = function() {
    console.log('🌐 Testing server connection...');
    
    if (window.AndroidIPConfig && window.AndroidIPConfig.testConnection) {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = '🔄 Testing...';
            statusElement.style.color = 'orange';
        }
        
        if (window.AndroidIPConfig && typeof window.AndroidIPConfig.testConnection === 'function') {
            window.AndroidIPConfig.testConnection();
            console.log('🌐 Connection test initiated - waiting for callback...');
        } else {
            console.error('🌐 AndroidIPConfig.testConnection not available');
            const statusElement = document.getElementById('connection-status');
            if (statusElement) {
                statusElement.textContent = '❌ Bridge not available';
                statusElement.style.color = 'red';
            }
        }
    } else {
        console.error('🌐 AndroidIPConfig.testConnection not available');
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = '❌ Bridge not available';
            statusElement.style.color = 'red';
        }
    }
};

// 📍 LOCATION HANDLER
class LocationHandler {
    constructor() {
        this.locationData = null;
        this.isReady = false;
        this.pendingCallbacks = [];
        
        window.locationReceiver = this.handleLocationResponse.bind(this);
        console.log('📍 LocationHandler initialized');
    }

    handleLocationResponse(response) {
        console.log('📍 Location response received:', response);
        
        if (response.error) {
            console.error('📍 Location error:', response.error);
            return;
        }

        this.locationData = {
            latitude: response.latitude,
            longitude: response.longitude,
            accuracy: response.accuracy,
            timestamp: response.timestamp,
            receivedAt: Date.now()
        };

        console.log('📍 Location data received:', this.locationData);
        this.isReady = true;
        
        this.pendingCallbacks.forEach(callback => callback(this.locationData));
        this.pendingCallbacks = [];
    }
}

// 🎤 MAIN VOICE RECOGNITION SERVICE
class VoiceRecognitionService {
    constructor() {
        this.recognition = null;
        this.synthesis = window.speechSynthesis;
        this.isListening = false;
        this.currentLanguage = 'en-US';
        this.isWebView = this.detectWebView();
        this.isIncognito = false;
        this.incognitoRetries = 0;
        this.maxIncognitoRetries = 3;
        
        this.initialize();
        this.setupGlobalSpeechHandler();
        this.setupEventListeners();
        this.setupButtons();
    }

    detectWebView() {
        return /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(navigator.userAgent.toLowerCase()) ||
               window.AndroidSpeechBridge !== undefined ||
               window.androidInterface !== undefined ||
               window.SpeechRecognitionInterface !== undefined;
    }

    initialize() {
        this.isIncognito = this.detectIncognito();
        
        if (this.isSupported()) {
            this.setupWebSpeechRecognition();
        }
        
        console.log('🔧 Voice recognition initialized - WebView:', this.isWebView);
        console.log('🔧 Incognito mode:', this.isIncognito);
        console.log('🔧 FLOW_STEP:', FLOW_STEP);
        console.log('🔧 Browser support:', this.isSupported());
        
        this.initializeVoices();
        
        if (this.isIncognito) {
            this.setupIncognitoMode();
        }
    }
    
    detectIncognito() {
        try {
            if ('webkitRequestFileSystem' in window) {
                window.webkitRequestFileSystem(window.TEMPORARY, 1, () => {}, () => {
                    return true;
                });
            }
            
            if ('storage' in navigator && 'estimate' in navigator.storage) {
                navigator.storage.estimate().then(estimate => {
                    if (estimate.quota < 120000000) {
                        return true;
                    }
                });
            }
            
            return !!(window.chrome && window.chrome.webstore) ? false : true;
        } catch (e) {
            return false;
        }
    }

    setupWebSpeechRecognition() {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) return;

        this.recognition = new SpeechRecognition();
        this.recognition.continuous = false;
        this.recognition.interimResults = true;
        this.recognition.lang = this.currentLanguage;

        this.recognition.onstart = () => {
            this.isListening = true;
            this.updateStatus('Listening...');
            this.updateButtonState(true);
        };

        this.recognition.onresult = (event) => {
            const current = event.resultIndex;
            const transcript = event.results[current][0].transcript;
            
            if (event.results[current].isFinal) {
                this.processRecognizedSpeech(transcript);
            } else {
                this.updatePartialResult(transcript);
            }
        };

        this.recognition.onerror = (event) => {
            this.isListening = false;
            this.updateStatus(`Error: ${event.error}`);
            this.updateButtonState(false);
        };

        this.recognition.onend = () => {
            this.isListening = false;
            this.updateStatus('Ready');
            this.updateButtonState(false);
        };
    }

    getAndroidInterface() {
        return window.AndroidSpeechBridge || window.androidInterface || window.SpeechRecognitionInterface;
    }

    setupGlobalSpeechHandler() {
        window.debugMobileInterface = () => {
            console.log('🔧 Mobile Interface Debug:');
            console.log('  - AndroidSpeechBridge:', !!window.AndroidSpeechBridge);
            console.log('  - androidInterface:', !!window.androidInterface);
            console.log('  - SpeechRecognitionInterface:', !!window.SpeechRecognitionInterface);
            console.log('  - getAndroidInterface():', !!this.getAndroidInterface());
            console.log('  - isWebView:', this.isWebView);
            console.log('  - User Agent:', navigator.userAgent);
            
            const androidInterface = this.getAndroidInterface();
            if (androidInterface) {
                console.log('  - Android interface methods:');
                console.log('    - startSpeechRecognition:', typeof androidInterface.startSpeechRecognition);
                console.log('    - stopSpeechRecognition:', typeof androidInterface.stopSpeechRecognition);
                console.log('    - speakNameInstruction:', typeof androidInterface.speakNameInstruction);
                console.log('    - speakStateInstruction:', typeof androidInterface.speakStateInstruction);
                console.log('    - speakCurrentQuestion:', typeof androidInterface.speakCurrentQuestion);
                console.log('    - speakNavigationInstruction:', typeof androidInterface.speakNavigationInstruction);
            }
        };

        window.fromNativeSpeech = (text) => {
            console.log('🗣️ Speech from native:', text);
            console.log('🔧 FLOW_STEP before handling:', FLOW_STEP);
            console.log('🔧 handleVoiceInput function exists:', typeof handleVoiceInput);
            
            const normalizedText = normalize(text);
            console.log('🔧 Normalized text:', normalizedText);
            
            if (typeof handleVoiceInput === 'function') {
                handleVoiceInput(normalizedText);
            } else {
                console.error('❌ handleVoiceInput function not found!');
            }
        };

        console.log('🔗 Native speech hooks installed');
    }

    setupEventListeners() {
        const nameInput = document.getElementById('userName');
        if (nameInput) {
            nameInput.addEventListener('input', (e) => {
                if (e.target.value.trim().length > 2) {
                    // Don't auto-advance
                }
            });
        }

        const stateSelect = document.getElementById('userState');
        if (stateSelect) {
            stateSelect.addEventListener('change', () => {
                // Don't auto-advance
            });
        }
    }

    setupButtons() {
        const voiceBtn = document.getElementById('voiceToggle');
        console.log('🔧 Setting up buttons - voiceToggle found:', !!voiceBtn);
        
        if (voiceBtn) {
            // Remove any existing listeners to prevent duplicates
            voiceBtn.onclick = null;
            voiceBtn.removeEventListener('click', voiceBtn._clickHandler);
            
            // Create new click handler
            voiceBtn._clickHandler = () => {
                console.log('🔧 Voice button clicked!');
                if (this.isWebView) {
                    startVoiceFlow();
                } else {
                    if (this.isListening) {
                        this.stopListening();
                    } else {
                        console.log('🔧 Starting desktop voice flow');
                        startVoiceFlow();
                    }
                }
            };
            
            // Add the click listener
            voiceBtn.addEventListener('click', voiceBtn._clickHandler);
            console.log('✅ Voice button click handler added');
        } else {
            console.log('❌ voiceToggle button not found!');
        }
        
        const speakBtn = document.getElementById('speakQuestion');
        if (speakBtn) {
            speakBtn.onclick = () => {
                this.speakCurrentQuestion();
            };
        }
    }

    processRecognizedSpeech(text) {
        const sanitizedText = this.sanitizeInput(text);
        const normalizedText = normalize(sanitizedText);
        
        console.log(`🗣️ Processing: "${sanitizedText}" → "${normalizedText}"`);
        
        handleVoiceInput(normalizedText);
    }

    updatePartialResult(text) {
        const sanitizedText = this.sanitizeInput(text);
        this.updateStatus(`Listening: "${text}"`);
        
        const activeInput = document.activeElement;
        if (activeInput && (activeInput.tagName === 'INPUT' || activeInput.tagName === 'TEXTAREA')) {
            activeInput.value = text;
        }
    }

    startListening() {
        console.log('🎤 startListening called');
        
        if (this.isWebView) {
            console.log('🎤 Starting Android speech recognition...');
            this.updateStatus('Initializing...');
            
            if (window.AndroidSpeechBridge) {
                try {
                    window.AndroidSpeechBridge.startSpeechRecognition();
                    console.log('🎤 Android speech recognition started');
                    return true;
                } catch (error) {
                    console.error('❌ Error starting Android speech recognition:', error);
                    return false;
                }
            } else {
                console.error('❌ AndroidSpeechBridge not available');
                return false;
            }
        } else {
            if (this.recognition && !this.isListening) {
                this.recognition.start();
                return true;
            }
        }
        return false;
    }

    stopListening() {
        if (this.isWebView) {
            if (window.AndroidSpeechBridge) {
                window.AndroidSpeechBridge.stopSpeechRecognition();
                console.log('🛑 Android speech recognition stopped');
                return true;
            }
        } else {
            if (this.recognition && this.isListening) {
                this.recognition.stop();
                return true;
            }
        }
        return false;
    }

    speakCurrentQuestion() {
        if (FLOW_STEP === 2) {
            const questionText = getCurrentQuestionText();
            if (this.isWebView) {
                if (window.AndroidSpeechBridge) {
                    window.AndroidSpeechBridge.speak(questionText, true); // Speak and then listen
                    console.log('✅ Question sent to Android TTS with listening');
                } else {
                    console.log('❌ AndroidSpeechBridge not available');
                }
            } else {
                this.speakText(questionText);
            }
        }
    }

    speakText(text) {
        if (!this.synthesis) {
            console.log('❌ Speech synthesis not supported');
            return;
        }

        this.synthesis.cancel();
        
        let voices = this.synthesis.getVoices();
        
        if (this.isIncognito && voices.length === 0) {
            console.log('🔧 Incognito mode: No voices available, retrying...');
            this.retrySpeakInIncognito(text);
            return;
        }
        
        if (voices.length === 0) {
            this.synthesis.addEventListener('voiceschanged', () => {
                voices = this.synthesis.getVoices();
                this.speakWithVoice(text, voices);
            });
        } else {
            this.speakWithVoice(text, voices);
        }
    }
    
    retrySpeakInIncognito(text) {
        if (this.incognitoRetries < this.maxIncognitoRetries) {
            this.incognitoRetries++;
            console.log(`🔧 Incognito retry ${this.incognitoRetries}/${this.maxIncognitoRetries}`);
            
            setTimeout(() => {
                const voices = this.synthesis.getVoices();
                if (voices.length > 0) {
                    console.log('🔧 Incognito: Voices loaded after retry');
                    this.speakWithVoice(text, voices);
                } else {
                    this.retrySpeakInIncognito(text);
                }
            }, 500 * this.incognitoRetries);
        } else {
            console.log('❌ Incognito: Max retries reached, using fallback');
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = this.currentLanguage;
            utterance.rate = 0.9;
            utterance.pitch = 0.8;
            utterance.volume = 1;
            this.synthesis.speak(utterance);
        }
    }
    
    speakWithVoice(text, voices) {
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = this.currentLanguage;
        
        let selectedVoice = null;
        
        // First try to find a male voice
        for (let voice of voices) {
            if (voice.lang.includes(this.currentLanguage.split('-')[0])) {
                const voiceName = voice.name.toLowerCase();
                if (voiceName.includes('male') || 
                    voiceName.includes('david') || 
                    voiceName.includes('alex') ||
                    voiceName.includes('google') ||
                    voiceName.includes('microsoft')) {
                    selectedVoice = voice;
                    console.log('🔊 Selected male voice:', voice.name);
                    break;
                }
            }
        }
        
        // If no male voice found, use first available voice
        if (!selectedVoice && voices.length > 0) {
            selectedVoice = voices[0];
            console.log('🔊 Using first available voice:', selectedVoice.name);
        }
        
        if (selectedVoice) {
            utterance.voice = selectedVoice;
        }
        
        utterance.rate = 0.9;
        utterance.pitch = 0.8;
        utterance.volume = 1;

        console.log('🔊 Speaking with voice:', selectedVoice?.name || 'default');
        this.synthesis.speak(utterance);
    }

    updateStatus(status) {
        const statusElement = document.getElementById('voiceStatus');
        if (statusElement) {
            statusElement.textContent = status;
        }
    }

    updateButtonState(isListening) {
        const btn = document.getElementById('voiceToggle');
        if (btn) {
            btn.textContent = isListening ? '🔴 Stop Voice' : '🎤 Start Voice';
            btn.classList.toggle('listening', isListening);
        }
    }

    isSupported() {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    }

    setLanguage(language) {
        this.currentLanguage = language;
        if (this.recognition) {
            this.recognition.lang = language;
        }
    }

    sanitizeInput(text) {
        return text.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
                 .replace(/<[^>]*>/g, '')
                 .trim();
    }

    onNativeSpeechError(error) {
        this.isListening = false;
        this.updateStatus(`Error: ${error}`);
        this.updateButtonState(false);
    }

    initializeVoices() {
        if (this.synthesis) {
            const loadVoices = () => {
                const voices = this.synthesis.getVoices();
                console.log('🔊 Available voices:', voices.length);
                if (voices.length > 0) {
                    console.log('🔊 First few voices:', voices.slice(0, 3).map(v => v.name));
                }
            };
            
            if (this.synthesis.getVoices().length > 0) {
                loadVoices();
            } else {
                this.synthesis.addEventListener('voiceschanged', loadVoices);
            }
        }
    }

    setupIncognitoMode() {
        console.log('🔧 Setting up incognito mode compatibility');
        
        if (this.synthesis) {
            const dummyUtterance = new SpeechSynthesisUtterance('');
            dummyUtterance.volume = 0;
            this.synthesis.speak(dummyUtterance);
            
            setTimeout(() => {
                this.synthesis.cancel();
                console.log('🔧 Incognito mode: Voice synthesis activated');
            }, 100);
        }
    }

    destroy() {
        if (this.recognition) {
            this.recognition.stop();
            this.recognition = null;
        }
        
        if (this.synthesis) {
            this.synthesis.cancel();
        }
        
        this.isListening = false;
        this.updateStatus('Ready');
        this.updateButtonState(false);
    }
}

// GLOBAL VOICE FLOW FUNCTION - ORIGINAL WORKING VERSION
window.startVoiceFlow = () => {
    console.log(`🔧 startVoiceFlow called - FLOW_STEP: ${FLOW_STEP}`, 'Stack:', new Error().stack);
    
    if (window.isSpeaking) {
        console.log('🔧 Already speaking, skipping');
        return;
    }
    
    window.isSpeaking = true;
    
    if (FLOW_STEP === 0) {
        console.log('🔧 Speaking name instruction');
        console.log('🔧 WebView:', window.voiceService?.isWebView);
        console.log('🔧 VoiceService available:', !!window.voiceService);
        
        let spoken = false;
        
        if (window.voiceService && window.voiceService.isWebView) {
            if (window.AndroidSpeechBridge) {
                window.AndroidSpeechBridge.speak("Please say your name", true); // Speak and then listen
                spoken = true;
                console.log('✅ Name instruction sent to Android TTS with listening');
            } else {
                console.log('❌ AndroidSpeechBridge not available');
            }
        } else {
            // Web interface - use TTS
            console.log('🔧 Using web TTS for name instruction');
            if (window.voiceService) {
                window.voiceService.speakText("Please say your name");
                spoken = true;
                console.log('✅ Name instruction sent to web TTS');
            } else {
                console.log('❌ voiceService not available for web TTS');
            }
        }
        
        // Fallback: Try direct TTS if nothing worked
        if (!spoken) {
            console.log('🔧 Trying fallback TTS...');
            try {
                const fallbackUtterance = new SpeechSynthesisUtterance("Please say your name");
                fallbackUtterance.rate = 0.9;
                fallbackUtterance.pitch = 0.8;
                fallbackUtterance.volume = 1;
                window.speechSynthesis.speak(fallbackUtterance);
                console.log('✅ Fallback TTS initiated');
            } catch (error) {
                console.log('❌ Fallback TTS failed:', error);
            }
        }
    } else if (FLOW_STEP === 1) {
        console.log('🔧 Speaking state instruction');
        console.log('🔧 WebView:', window.voiceService?.isWebView);
        console.log('🔧 VoiceService available:', !!window.voiceService);
        
        let spoken = false;
        
        if (window.voiceService && window.voiceService.isWebView) {
            if (window.AndroidSpeechBridge) {
                window.AndroidSpeechBridge.speak("Please select your state", true); // Speak and then listen
                spoken = true;
                console.log('✅ State instruction sent to Android TTS with listening');
            } else {
                console.log('❌ AndroidSpeechBridge not available');
            }
        } else {
            // Web interface - use TTS
            console.log('🔧 Using web TTS for state instruction');
            if (window.voiceService) {
                window.voiceService.speakText("Please select your state");
                spoken = true;
                console.log('✅ State instruction sent to web TTS');
            } else {
                console.log('❌ voiceService not available for web TTS');
            }
        }
        
        // Fallback: Try direct TTS if nothing worked
        if (!spoken) {
            console.log('🔧 Trying fallback TTS...');
            try {
                const fallbackUtterance = new SpeechSynthesisUtterance("Please select your state");
                fallbackUtterance.rate = 0.9;
                fallbackUtterance.pitch = 0.8;
                fallbackUtterance.volume = 1;
                window.speechSynthesis.speak(fallbackUtterance);
                console.log('✅ Fallback TTS initiated');
            } catch (error) {
                console.log('❌ Fallback TTS failed:', error);
            }
        }
    } else if (FLOW_STEP === 2) {
        console.log('🔧 Speaking question');
        
        // Debug: Check what's on the page
        console.log('🔧 Page title:', document.title);
        console.log('🔧 Page URL:', window.location.href);
        console.log('🔧 All elements with input:', document.querySelectorAll('input').length);
        console.log('🔧 All select elements:', document.querySelectorAll('select').length);
        console.log('🔧 All textarea elements:', document.querySelectorAll('textarea').length);
        
        const currentQuestion = findCurrentQuestion();
        console.log('🔧 Current question found:', !!currentQuestion);
        console.log('🔧 All questions on page:', document.querySelectorAll('.question-item').length);
        console.log('🔧 Unanswered questions:', Array.from(document.querySelectorAll('.question-item')).filter(q => !q.classList.contains('answered')).length);
        
        // Try different selectors to find questions
        const allPossibleQuestions = [
            ...document.querySelectorAll('.question-item'),
            ...document.querySelectorAll('.question'),
            ...document.querySelectorAll('.survey-question'),
            ...document.querySelectorAll('[data-question-id]'),
            ...document.querySelectorAll('.form-group')
        ];
        console.log('🔧 All possible question elements:', allPossibleQuestions.length);
        console.log('🔧 First few question elements:', allPossibleQuestions.slice(0, 3).map(q => ({
            classes: q.className,
            tag: q.tagName,
            id: q.id,
            text: q.textContent?.substring(0, 50)
        })));
        
        if (currentQuestion) {
            const questionText = getCurrentQuestionText();
            console.log('🔧 Question text to speak:', questionText);
            console.log('🔧 Question element:', currentQuestion);
            console.log('🔧 Question HTML:', currentQuestion.innerHTML.substring(0, 200) + '...');
            
            if (window.voiceService && window.voiceService.isWebView) {
                if (window.AndroidSpeechBridge) {
                    window.AndroidSpeechBridge.speak(questionText, true); // Speak and then listen
                    console.log('✅ Question sent to Android TTS with listening');
                } else {
                    console.log('❌ AndroidSpeechBridge not available, using web TTS');
                    if (window.voiceService) {
                        window.voiceService.speakText(questionText);
                    }
                }
            } else {
                // Web interface - use TTS
                if (window.voiceService) {
                    window.voiceService.speakText(questionText);
                }
            }
        } else {
            console.log('❌ No current question found, checking if all done');
            if (!hasUnansweredQuestions()) {
                console.log('🔧 All questions completed, moving to pre-submit phase');
                FLOW_STEP = 3;
                setTimeout(() => {
                    startVoiceFlow();
                }, 1000);
            } else {
                console.log('❌ Questions exist but could not find current one, retrying');
                if (!window.flowRetryCount) window.flowRetryCount = 0;
                window.flowRetryCount++;
                
                if (window.flowRetryCount < 3) {
                    setTimeout(() => {
                        startVoiceFlow();
                    }, 1000);
                } else {
                    console.log('❌ Max retries reached, moving to submit');
                    FLOW_STEP = 3;
                    setTimeout(() => {
                        startVoiceFlow();
                    }, 1000);
                }
            }
        }
    } else if (FLOW_STEP === 3) {
        console.log('🔧 Speaking pre-submit instruction');
        if (window.voiceService && window.voiceService.isWebView) {
            if (window.AndroidSpeechBridge) {
                window.AndroidSpeechBridge.speak("All questions completed. Say next to go to the final stage, or say submit to finish now", true);
                console.log('✅ Pre-submit instruction sent to Android TTS with listening');
            } else {
                console.log('❌ AndroidSpeechBridge not available, using web TTS');
                if (window.voiceService) {
                    window.voiceService.speakText("All questions completed. Say next to go to the final stage, or say submit to finish now");
                }
            }
        } else {
            // Web interface - use TTS
            if (window.voiceService) {
                window.voiceService.speakText("All questions completed. Say next to go to the final stage, or say submit to finish now");
            }
        }
    } else if (FLOW_STEP === 4) {
        console.log('🔧 Speaking final submit instruction');
        if (window.voiceService && window.voiceService.isWebView) {
            if (window.AndroidSpeechBridge) {
                window.AndroidSpeechBridge.speak("Say submit to finish and submit your response", true);
                console.log('✅ Final submit instruction sent to Android TTS with listening');
            } else {
                console.log('❌ AndroidSpeechBridge not available, using web TTS');
                if (window.voiceService) {
                    window.voiceService.speakText("Say submit to finish and submit your response");
                }
            }
        } else {
            // Web interface - use TTS
            if (window.voiceService) {
                window.voiceService.speakText("Say submit to finish and submit your response");
            }
        }
    }
    
    // CRITICAL: Reset speaking flag after delay
    setTimeout(() => {
        console.log('🔧 Resetting isSpeaking flag');
        window.isSpeaking = false;
        
        // Process any pending voice inputs that came in while speaking
        if (window.pendingVoiceInput && window.pendingVoiceInput.length > 0) {
            console.log('🔧 Processing pending voice inputs:', window.pendingVoiceInput.length);
            const pendingInputs = [...window.pendingVoiceInput];
            window.pendingVoiceInput = [];
            
            // Process each pending input with a small delay
            pendingInputs.forEach((input, index) => {
                setTimeout(() => {
                    console.log('🔧 Processing pending input:', input);
                    handleVoiceInput(input);
                }, (index + 1) * 500); // 500ms between each
            });
        }
    }, 2000);
};

// Helper functions
function normalize(text) {
    return text.toLowerCase().trim()
        .replace(/[^\w\s]/g, '')
        .replace(/\s+/g, ' ');
}

function findCurrentQuestion() {
    console.log('🔧 Searching for questions on page...');
    
    // Try multiple possible selectors for questions
    const possibleSelectors = [
        '.question-item',
        '.question',
        '.survey-question', 
        '[data-question-id]',
        '.form-group',
        '.mb-3', // Bootstrap form groups
        '.card',  // Card-based questions
        '.form-check', // Radio/checkbox groups
        'fieldset',     // Fieldset-based questions
        '.survey-form > div', // Direct children of survey form
        'div[class*="question"]', // Any div with "question" in class
        'div[id*="question"]'    // Any div with "question" in id
    ];
    
    console.log('🔧 Page elements found:');
    possibleSelectors.forEach(selector => {
        const elements = document.querySelectorAll(selector);
        if (elements.length > 0) {
            console.log(`  ${selector}: ${elements.length} elements`);
        }
    });
    
    for (let selector of possibleSelectors) {
        const questions = document.querySelectorAll(selector);
        console.log(`🔧 Trying selector "${selector}": found ${questions.length} elements`);
        
        for (let question of questions) {
            // Check if this element actually contains a question
            const hasQuestionText = question.querySelector('label, .question-text, h4, h5, h6, p, legend');
            const hasInputs = question.querySelector('input, select, textarea');
            const isAnswered = question.classList.contains('answered') || 
                              question.querySelector('.answered') ||
                              question.querySelector('input:checked, input[type="radio"]:checked, input[type="checkbox"]:checked');
            
            console.log(`🔧 Element check - hasText: ${!!hasQuestionText}, hasInputs: ${!!hasInputs}, isAnswered: ${isAnswered}`);
            
            if (hasQuestionText && hasInputs && !isAnswered) {
                console.log('✅ Found unanswered question with selector:', selector);
                console.log('🔧 Question element:', question);
                return question;
            }
        }
    }
    
    // If no specific questions found, try to find any form with inputs
    console.log('🔧 No specific questions found, checking for any form inputs...');
    const allInputs = document.querySelectorAll('input:not([type="hidden"]), select, textarea');
    console.log(`🔧 Found ${allInputs.length} total form inputs`);
    
    for (let input of allInputs) {
        const parent = input.closest('div, fieldset, form-group, .mb-3, .card');
        if (parent && !parent.classList.contains('answered')) {
            console.log('✅ Using fallback question detection');
            return parent;
        }
    }
    
    console.log('❌ No unanswered questions found with any selector');
    return null;
}

function getCurrentQuestionText() {
    const currentQuestion = findCurrentQuestion();
    if (currentQuestion) {
        console.log('🔧 Extracting text from question element:', currentQuestion);
        
        // Try multiple selectors for question text
        const textSelectors = [
            '.question-text',
            'label',
            'h4', 'h5', 'h6',
            'p',
            'legend',
            '.form-label',
            '[class*="question"]',
            '[class*="title"]'
        ];
        
        for (let selector of textSelectors) {
            const textElement = currentQuestion.querySelector(selector);
            if (textElement && textElement.textContent.trim()) {
                const text = textElement.textContent.trim();
                console.log(`🔧 Found question text with selector "${selector}":`, text);
                return text;
            }
        }
        
        // Fallback: use the element's own text content
        const ownText = currentQuestion.textContent.trim();
        if (ownText) {
            console.log('🔧 Using element text content as fallback:', ownText);
            return ownText;
        }
    }
    
    console.log('❌ No question text found');
    return '';
}

function hasUnansweredQuestions() {
    console.log('🔧 Checking for unanswered questions...');
    
    // Use the same comprehensive detection as findCurrentQuestion
    const possibleSelectors = [
        '.question-item',
        '.question',
        '.survey-question', 
        '[data-question-id]',
        '.form-group',
        '.mb-3',
        '.card',
        '.form-check',
        'fieldset',
        '.survey-form > div',
        'div[class*="question"]',
        'div[id*="question"]'
    ];
    
    for (let selector of possibleSelectors) {
        const questions = document.querySelectorAll(selector);
        for (let question of questions) {
            const hasQuestionText = question.querySelector('label, .question-text, h4, h5, h6, p, legend');
            const hasInputs = question.querySelector('input, select, textarea');
            const isAnswered = question.classList.contains('answered') || 
                              question.querySelector('.answered') ||
                              question.querySelector('input:checked, input[type="radio"]:checked, input[type="checkbox"]:checked');
            
            if (hasQuestionText && hasInputs && !isAnswered) {
                console.log('✅ Found unanswered question');
                return true;
            }
        }
    }
    
    console.log('🔧 No unanswered questions found');
    return false;
}

function handleVoiceInput(text) {
    console.log('🎯 handleVoiceInput called immediately with:', text);
    console.log('🎯 Current FLOW_STEP at function start:', FLOW_STEP);
    console.log('🎯 handleVoiceInput function exists:', typeof handleVoiceInput);
    console.log('🎯 About to check FLOW_STEP conditions...');
    
    if (FLOW_STEP === 0) {
        console.log('🔧 Entering FLOW_STEP 0 (name processing)');
        const nameInput = document.getElementById('userName');
        if (nameInput) {
            nameInput.value = text;
            nameInput.dispatchEvent(new Event('input'));
            FLOW_STEP = 1;
            setTimeout(() => {
                startVoiceFlow();
            }, 1000);
        }
    } else if (FLOW_STEP === 1) {
        console.log('🔧 Entering FLOW_STEP 1 (state processing)');
        console.log('🎯 Processing state input:', text);
        const stateSelect = document.getElementById('userState');
        console.log('🔧 State select element found:', !!stateSelect);
        
        if (stateSelect) {
            console.log('🔧 Current state select value:', stateSelect.value);
            console.log('🔧 Available options:', Array.from(stateSelect.options).map(opt => ({ value: opt.value, text: opt.text })));
            // Check if any option has Maharashtra
const maharashtraOption = Array.from(stateSelect.options).find(opt => 
    opt.text.toLowerCase().includes('maharashtra')
);
console.log('Maharashtra option details:', {
    value: maharashtraOption?.value,
    text: maharashtraOption?.text,
    innerHTML: maharashtraOption?.innerHTML
});
            
            const stateMap = {
                'andhra pradesh': 'Andhra Pradesh',
                'arunachal pradesh': 'Arunachal Pradesh',
                'assam': 'Assam',
                'bihar': 'Bihar',
                'chhattisgarh': 'Chhattisgarh',
                'goa': 'Goa',
                'gujarat': 'Gujarat',
                'haryana': 'Haryana',
                'himachal pradesh': 'Himachal Pradesh',
                'jharkhand': 'Jharkhand',
                'karnataka': 'Karnataka',
                'kerala': 'Kerala',
                'madhya pradesh': 'Madhya Pradesh',
                'maharashtra': 'Maharashtra',
                'manipur': 'Manipur',
                'meghalaya': 'Meghalaya',
                'mizoram': 'Mizoram',
                'nagaland': 'Nagaland',
                'odisha': 'Odisha',
                'punjab': 'Punjab',
                'rajasthan': 'Rajasthan',
                'sikkim': 'Sikkim',
                'tamil nadu': 'Tamil Nadu',
                'telangana': 'Telangana',
                'tripura': 'Tripura',
                'uttar pradesh': 'Uttar Pradesh',
                'uttarakhand': 'Uttarakhand',
                'west bengal': 'West Bengal',
                'delhi': 'Delhi',
                'jammu & kashmir': 'Jammu & Kashmir',
                'ladakh': 'Ladakh',
                'puducherry': 'Puducherry'
            };
            
            const normalizedText = normalize(text);
            console.log('🔧 Normalized state text:', normalizedText);
            
            // Check for exact match first - iterate through states in order
                let exactMatchFound = false;
                for (let [state, fullName] of Object.entries(stateMap)) {
                    const stateLower = state.toLowerCase();
                    const fullNameLower = fullName.toLowerCase();
                    const textLower = normalizedText.toLowerCase();
                    
                    console.log(`🔧 Checking: "${textLower}" against "${stateLower}" or "${fullNameLower}"`);
                    
                    // Exact match check
                    if (textLower === stateLower || textLower === fullNameLower) {
                        console.log(`✅ Exact state match found: "${state}" -> "${fullName}"`);
                        
                        // Find and set option by text instead of value
                        for (let i = 0; i < stateSelect.options.length; i++) {
                            if (stateSelect.options[i].text === fullName) {
                                stateSelect.selectedIndex = i;
                                stateSelect.value = fullName;
                                console.log('🔧 Option found at index:', i, 'with text:', stateSelect.options[i].text);
                                break;
                            }
                        }
                        
                        stateSelect.dispatchEvent(new Event('change'));
                        console.log('🔧 State select value set to:', stateSelect.value);
                        console.log('🔧 Selected index:', stateSelect.selectedIndex);
                        console.log('🔧 Change event dispatched');
                        
                        FLOW_STEP = 2;
                        console.log('🔧 FLOW_STEP advanced to:', FLOW_STEP);
                        
                        setTimeout(() => {
                            console.log('🔧 Starting next voice flow...');
                            startVoiceFlow();
                        }, 1000);
                        exactMatchFound = true;
                        break;
                    }
                }
                
                if (exactMatchFound) return;
            
            console.log('❌ No state match found for:', normalizedText);
            console.log('🔧 Available states to try:', Object.keys(stateMap).join(', '));
        } else {
            console.log('❌ State select element not found!');
        }
    } else if (FLOW_STEP === 2) {
        console.log('🎯 Processing question input:', text);
        const currentQuestion = findCurrentQuestion();
        console.log('🔧 Current question found:', !!currentQuestion);
        
        if (currentQuestion) {
            const radioButtons = currentQuestion.querySelectorAll('input[type="radio"]');
            const checkboxes = currentQuestion.querySelectorAll('input[type="checkbox"]');
            const textInput = currentQuestion.querySelector('input[type="text"], textarea');
            const scaleInputs = currentQuestion.querySelectorAll('input[type="number"], input[type="range"]');
            
            console.log('🔧 Question elements - radios:', radioButtons.length, 'checkboxes:', checkboxes.length, 'text:', !!textInput, 'scale:', scaleInputs.length);
            
            // Handle single choice and true-false (radio buttons)
            if (radioButtons.length > 0) {
                const normalizedText = normalize(text);
                console.log('🔧 Processing radio buttons for:', normalizedText);
                
                for (let radio of radioButtons) {
                    const label = radio.parentElement.textContent.toLowerCase().trim();
                    const value = radio.value?.toLowerCase() || '';
                    console.log(`🔧 Checking radio: "${label}" / "${value}" against "${normalizedText}"`);
                    
                    if (normalizedText.includes(label) || normalizedText.includes(value) || 
                        (normalizedText.includes('yes') && value.includes('true')) ||
                        (normalizedText.includes('no') && value.includes('false')) ||
                        (normalizedText.includes('true') && value.includes('true')) ||
                        (normalizedText.includes('false') && value.includes('false'))) {
                        radio.checked = true;
                        radio.dispatchEvent(new Event('change'));
                        console.log('✅ Radio selected:', label);
                        currentQuestion.classList.add('answered');
                        setTimeout(() => {
                            startVoiceFlow();
                        }, 1000);
                        return;
                    }
                }
            }
            // Handle multiple choice (checkboxes)
            else if (checkboxes.length > 0) {
                const normalizedText = normalize(text);
                const words = normalizedText.split(' ');
                console.log('🔧 Processing checkboxes for:', normalizedText);
                
                let anyChecked = false;
                for (let word of words) {
                    for (let checkbox of checkboxes) {
                        const label = checkbox.parentElement.textContent.toLowerCase().trim();
                        if (label.includes(word) || word.includes(label)) {
                            checkbox.checked = true;
                            checkbox.dispatchEvent(new Event('change'));
                            anyChecked = true;
                            console.log('✅ Checkbox selected:', label);
                        }
                    }
                }
                
                if (anyChecked) {
                    currentQuestion.classList.add('answered');
                    setTimeout(() => {
                        startVoiceFlow();
                    }, 1000);
                    return;
                }
            }
            // Handle scale/rating questions
            else if (scaleInputs.length > 0) {
                const normalizedText = normalize(text);
                console.log('🔧 Processing scale for:', normalizedText);
                
                // Extract numbers from text
                const numbers = normalizedText.match(/\d+/);
                if (numbers && numbers[0]) {
                    const rating = parseInt(numbers[0]);
                    console.log('🔧 Extracted rating:', rating);
                    
                    for (let input of scaleInputs) {
                        const min = parseInt(input.min) || 1;
                        const max = parseInt(input.max) || 10;
                        if (rating >= min && rating <= max) {
                            input.value = rating;
                            input.dispatchEvent(new Event('change'));
                            console.log('✅ Scale set to:', rating);
                            currentQuestion.classList.add('answered');
                            setTimeout(() => {
                                startVoiceFlow();
                            }, 1000);
                            return;
                        }
                    }
                }
            }
            // Handle text/descriptive questions
            else if (textInput) {
                console.log('🔧 Processing text input for:', text);
                textInput.value = text;
                textInput.dispatchEvent(new Event('input'));
                console.log('✅ Text set to:', text);
                currentQuestion.classList.add('answered');
                setTimeout(() => {
                    startVoiceFlow();
                }, 1000);
                return;
            }
        } else {
            console.log('❌ No current question found');
        }
    } else if (FLOW_STEP === 3) {
        const normalizedText = normalize(text);
        console.log('🎯 Processing pre-submit input:', normalizedText);
        
        if (normalizedText.includes('next')) {
            console.log('✅ Next command recognized, moving to final submit phase');
            FLOW_STEP = 4;
            setTimeout(() => {
                startVoiceFlow();
            }, 1000);
        } else if (normalizedText.includes('submit') || normalizedText.includes('finish') || 
            normalizedText.includes('done') || normalizedText.includes('complete')) {
            console.log('✅ Submit command recognized in pre-submit phase');
            const submitBtn = document.querySelector('button[type="submit"], .submit-btn, .btn-submit');
            if (submitBtn) {
                console.log('🔧 Submitting form...');
                submitBtn.click();
                
                // Reset flow after submission
                setTimeout(() => {
                    FLOW_STEP = 0;
                    window.isSpeaking = false;
                    console.log('🔄 Voice flow reset after submission');
                }, 2000);
            } else {
                console.log('❌ Submit button not found');
            }
        } else {
            console.log('❌ Next or submit command not recognized in:', normalizedText);
        }
    } else if (FLOW_STEP === 4) {
        const normalizedText = normalize(text);
        console.log('🎯 Processing final submit input:', normalizedText);
        
        if (normalizedText.includes('submit') || normalizedText.includes('finish') || 
            normalizedText.includes('done') || normalizedText.includes('complete')) {
            console.log('✅ Final submit command recognized');
            const submitBtn = document.querySelector('button[type="submit"], .submit-btn, .btn-submit');
            if (submitBtn) {
                console.log('🔧 Submitting form...');
                submitBtn.click();
                
                // Reset flow after submission
                setTimeout(() => {
                    FLOW_STEP = 0;
                    window.isSpeaking = false;
                    console.log('🔄 Voice flow reset after submission');
                }, 2000);
            } else {
                console.log('❌ Submit button not found');
            }
        } else {
            console.log('❌ Submit command not recognized in:', normalizedText);
        }
    }
}

function toggleVoiceRecognition() {
    if (window.voiceService) {
        if (window.voiceService.isListening) {
            window.voiceService.stopListening();
        } else {
            window.voiceService.startListening();
        }
    }
}

function speakCurrentQuestion() {
    if (window.voiceService) {
        window.voiceService.speakCurrentQuestion();
    }
}

// Initialize everything when Android bridges are ready
// NOTE: DOMContentLoaded logic moved to onAndroidBridgeReady()
// No auto-initialization - wait for Android bridge ready signal

// FALLBACK: If Android doesn't call onAndroidBridgeReady within 5 seconds, initialize anyway
setTimeout(() => {
    if (!window.androidBridgeReadyCalled) {
        console.log('🚨 Android bridge ready not called, initializing fallback...');
        window.onAndroidBridgeReady();
    }
}, 5000);

// Track if Android bridge ready was called
window.androidBridgeReadyCalled = false;

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    if (e.ctrlKey && e.key === 'v') {
        e.preventDefault();
        toggleVoiceRecognition();
    } else if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        speakCurrentQuestion();
    }
});

// Test function
window.testUpdatedVoiceFlow = function() {
    console.log('🧪 Testing updated voice flow with original web interface...');
    
    window.speechReady = false;
    window.pendingSpeechOperation = null;
    window.isSpeaking = false;
    
    startVoiceFlow();
};

// Force start voice flow - use this if auto-start doesn't work
window.forceStartVoice = function() {
    console.log('🚀 Force starting voice flow...');
    
    // Reset flags
    window.speechReady = false;
    window.pendingSpeechOperation = null;
    window.isSpeaking = false;
    FLOW_STEP = 0;
    
    // Force start
    setTimeout(() => {
        startVoiceFlow();
    }, 500);
};

// Test state selection manually
window.testStateSelection = function(stateName) {
    console.log('🧪 Testing state selection with:', stateName);
    
    const stateSelect = document.getElementById('userState');
    if (!stateSelect) {
        console.log('❌ State select element not found!');
        return;
    }
    
    console.log('🔧 Available options:', Array.from(stateSelect.options).map(opt => ({ value: opt.value, text: opt.text })));
    
    const stateMap = {
        'andhra pradesh': 'AP',
        'arunachal pradesh': 'AR',
        'assam': 'AS',
        'bihar': 'BR',
        'chhattisgarh': 'CG',
        'goa': 'GA',
        'gujarat': 'GJ',
        'haryana': 'HR',
        'himachal pradesh': 'HP',
        'jharkhand': 'JH',
        'karnataka': 'KA',
        'kerala': 'KL',
        'madhya pradesh': 'MP',
        'maharashtra': 'MH',
        'manipur': 'MN',
        'meghalaya': 'ML',
        'mizoram': 'MZ',
        'nagaland': 'NL',
        'odisha': 'OR',
        'punjab': 'PB',
        'rajasthan': 'RJ',
        'sikkim': 'SK',
        'tamil nadu': 'TN',
        'telangana': 'TG',
        'tripura': 'TR',
        'uttar pradesh': 'UP',
        'uttarakhand': 'UK',
        'west bengal': 'WB',
        'delhi': 'DL',
        'jammu & kashmir': 'JK',
        'ladakh': 'LA',
        'puducherry': 'PY'
    };
    
    const normalizedText = normalize(stateName);
    console.log('🔧 Normalized text:', normalizedText);
    
    // Check exact match first (highest priority)
    for (let [fullName, code] of Object.entries(stateMap)) {
        if (normalizedText === fullName.toLowerCase()) {
            console.log(`✅ Exact state match found: "${fullName}" -> "${code}"`);
            
            // Set by selectedIndex for better compatibility
            for (let i = 0; i < stateSelect.options.length; i++) {
                if (stateSelect.options[i].text === fullName || stateSelect.options[i].value === fullName) {
                    stateSelect.selectedIndex = i;
                    stateSelect.value = stateSelect.options[i].value;
                    console.log(`🔧 State selected: index ${i}, value "${stateSelect.options[i].value}"`);
                    break;
                }
            }
            
            stateSelect.dispatchEvent(new Event('change'));
            FLOW_STEP = 2;
            setTimeout(() => {
                startVoiceFlow();
            }, 1000);
            return;
        }
    }

    // Check if text contains state name (medium priority)
    for (let [fullName, code] of Object.entries(stateMap)) {
        if (normalizedText.includes(fullName.toLowerCase()) || fullName.toLowerCase().includes(normalizedText)) {
            console.log(`✅ Partial state match found: "${fullName}" -> "${code}"`);
            
            // Set by selectedIndex for better compatibility
            for (let i = 0; i < stateSelect.options.length; i++) {
                if (stateSelect.options[i].text === fullName || stateSelect.options[i].value === fullName) {
                    stateSelect.selectedIndex = i;
                    stateSelect.value = stateSelect.options[i].value;
                    console.log(`🔧 State selected: index ${i}, value "${stateSelect.options[i].value}"`);
                    break;
                }
            }
            
            stateSelect.value = fullName; // Use full name as value
            stateSelect.dispatchEvent(new Event('change'));
            FLOW_STEP = 2;
            setTimeout(() => {
                startVoiceFlow();
            }, 1000);
            return;
        }
    }

    // Check code match (lowest priority)
    for (let [fullName, code] of Object.entries(stateMap)) {
        if (normalizedText === code.toLowerCase()) {
            console.log(`✅ State code match found: "${fullName}" -> "${code}"`);
            stateSelect.value = fullName; // Use full name as value
            stateSelect.dispatchEvent(new Event('change'));
            FLOW_STEP = 2;
            setTimeout(() => {
                startVoiceFlow();
            }, 1000);
            return;
        }
    }
    
    console.log('❌ No state match found for:', normalizedText);
};