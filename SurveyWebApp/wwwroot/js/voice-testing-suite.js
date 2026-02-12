// Complete Voice Flow Testing Suite
window.testCompleteVoiceFlow = function() {
    console.log('🧪 === COMPLETE VOICE FLOW TEST ===');
    
    // Step 1: Test Name Input
    console.log('📝 Step 1: Testing name input...');
    FLOW_STEP = 0;
    window.onSpeechEnd("John Doe");
    
    setTimeout(() => {
        console.log('📝 Step 2: Testing state input...');
        window.onSpeechEnd("maharashtra");
        
        setTimeout(() => {
            console.log('📝 Step 3: Testing question answers...');
            testAllQuestionTypes();
        }, 2000);
    }, 2000);
};

// Test all question types
window.testAllQuestionTypes = function() {
    console.log('🧪 === TESTING ALL QUESTION TYPES ===');
    
    // Test Single Choice (Radio)
    console.log('📝 Testing Single Choice...');
    simulateAnswer("very satisfied");
    
    setTimeout(() => {
        // Test Multiple Choice (Checkbox)
        console.log('📝 Testing Multiple Choice...');
        simulateAnswer("option 1 and option 3");
        
        setTimeout(() => {
            // Test Text Input
            console.log('📝 Testing Text Input...');
            simulateAnswer("this is a text answer");
            
            setTimeout(() => {
                // Test Submit
                console.log('📝 Testing Submit...');
                simulateAnswer("submit");
            }, 2000);
        }, 2000);
    }, 2000);
};

// Simulate answer for current question
window.simulateAnswer = function(answer) {
    console.log('🎤 Simulating answer:', answer);
    window.onSpeechEnd(answer);
};

// Test state selection specifically
window.testStateSelectionFlow = function() {
    console.log('🧪 === STATE SELECTION TEST ===');
    
    // Set to state step
    FLOW_STEP = 1;
    
    // Test various states
    const testStates = ["maharashtra", "karnataka", "delhi", "tamil nadu"];
    let index = 0;
    
    function testNextState() {
        if (index < testStates.length) {
            console.log(`📝 Testing state: ${testStates[index]}`);
            window.onSpeechEnd(testStates[index]);
            index++;
            
            setTimeout(() => {
                // Reset for next test
                FLOW_STEP = 1;
                testNextState();
            }, 1000);
        } else {
            console.log('✅ State selection tests complete!');
        }
    }
    
    testNextState();
};

// Test error handling
window.testErrorHandling = function() {
    console.log('🧪 === ERROR HANDLING TEST ===');
    
    // Test empty speech
    window.onSpeechEnd("");
    
    setTimeout(() => {
        // Test undefined speech
        window.onSpeechEnd(undefined);
        
        setTimeout(() => {
            // Test error callback
            window.onSpeechError("Network error");
        }, 1000);
    }, 1000);
};

// Test TTS functionality
window.testTTSFlow = function() {
    console.log('🧪 === TTS FLOW TEST ===');
    
    if (window.AndroidSpeechBridge) {
        console.log('🗣️ Testing TTS...');
        
        // Test name instruction
        window.AndroidSpeechBridge.speak("Please say your name", true);
        
        setTimeout(() => {
            // Test state instruction
            window.AndroidSpeechBridge.speak("Please say your state", true);
            
            setTimeout(() => {
                // Test question instruction
                window.AndroidSpeechBridge.speak("How satisfied are you?", true);
            }, 3000);
        }, 3000);
    } else {
        console.log('❌ AndroidSpeechBridge not available');
    }
};

// Debug current state
window.debugCurrentState = function() {
    console.log('🔍 === CURRENT STATE DEBUG ===');
    console.log('FLOW_STEP:', FLOW_STEP);
    console.log('VOICE_SERVICE:', !!window.voiceService);
    console.log('IS_LISTENING:', window.isListening);
    console.log('ANDROID_BRIDGES:', {
        speech: !!window.AndroidSpeechBridge,
        location: !!window.AndroidLocationBridge,
        ip: !!window.AndroidIPConfig
    });
    
    // Check current question
    const currentQuestion = findCurrentQuestion();
    if (currentQuestion) {
        console.log('CURRENT_QUESTION:', {
            type: currentQuestion.querySelector('input[type="radio"]') ? 'SingleChoice' :
                 currentQuestion.querySelector('input[type="checkbox"]') ? 'MultipleChoice' :
                 currentQuestion.querySelector('input[type="text"], textarea') ? 'Text' : 'Unknown',
            hasOptions: currentQuestion.querySelectorAll('input').length > 0
        });
    }
    
    // Check form elements
    console.log('FORM_ELEMENTS:', {
        nameInput: !!document.getElementById('userName'),
        stateSelect: !!document.getElementById('userState'),
        submitButton: !!document.querySelector('button[type="submit"]')
    });
};

// Quick flow test
window.quickFlowTest = function() {
    console.log('🚀 === QUICK FLOW TEST ===');
    
    // Test complete flow in sequence
    const sequence = [
        { step: "name", text: "John Doe", delay: 0 },
        { step: "state", text: "maharashtra", delay: 2000 },
        { step: "question1", text: "very satisfied", delay: 4000 },
        { step: "question2", text: "good", delay: 6000 },
        { step: "submit", text: "submit", delay: 8000 }
    ];
    
    sequence.forEach(item => {
        setTimeout(() => {
            console.log(`🎤 ${item.step}: ${item.text}`);
            window.onSpeechEnd(item.text);
        }, item.delay);
    });
};

// Test specific scenarios
window.testScenarios = {
    // Test all Indian states
    testAllStates: function() {
        console.log('🧪 === TESTING ALL STATES ===');
        const states = [
            "andhra pradesh", "arunachal pradesh", "assam", "bihar", "chhattisgarh",
            "goa", "gujarat", "haryana", "himachal pradesh", "jharkhand",
            "karnataka", "kerala", "madhya pradesh", "maharashtra", "manipur",
            "meghalaya", "mizoram", "nagaland", "odisha", "punjab",
            "rajasthan", "sikkim", "tamil nadu", "telangana", "tripura",
            "uttar pradesh", "uttarakhand", "west bengal", "delhi", "jammu & kashmir",
            "ladakh", "puducherry"
        ];
        
        states.forEach((state, index) => {
            setTimeout(() => {
                FLOW_STEP = 1;
                window.onSpeechEnd(state);
                console.log(`📝 Tested: ${state}`);
            }, index * 500);
        });
    },
    
    // Test different name formats
    testNames: function() {
        console.log('🧪 === TESTING NAME FORMATS ===');
        const names = [
            "John", "John Doe", "John Smith", "Mary", "Mary Jane", "Mary Ann Smith",
            "Rahul", "Rahul Sharma", "Priya Patel", "Amit Kumar Singh"
        ];
        
        names.forEach((name, index) => {
            setTimeout(() => {
                FLOW_STEP = 0;
                window.onSpeechEnd(name);
                console.log(`📝 Tested name: ${name}`);
            }, index * 500);
        });
    },
    
    // Test question answers
    testAnswers: function() {
        console.log('🧪 === TESTING QUESTION ANSWERS ===');
        const answers = [
            "very satisfied", "satisfied", "neutral", "dissatisfied", "very dissatisfied",
            "yes", "no", "maybe", "option 1", "option 2", "option 3",
            "this is a text answer", "I like it", "it is good", "excellent service"
        ];
        
        answers.forEach((answer, index) => {
            setTimeout(() => {
                FLOW_STEP = 2;
                window.onSpeechEnd(answer);
                console.log(`📝 Tested answer: ${answer}`);
            }, index * 500);
        });
    }
};

console.log('🧪 Voice testing suite loaded!');
console.log('Available tests:');
console.log('- testCompleteVoiceFlow() - Full end-to-end test');
console.log('- testAllQuestionTypes() - Test all question types');
console.log('- testStateSelectionFlow() - Test state selection');
console.log('- testErrorHandling() - Test error scenarios');
console.log('- testTTSFlow() - Test text-to-speech');
console.log('- debugCurrentState() - Debug current state');
console.log('- quickFlowTest() - Quick sequence test');
console.log('- testScenarios.testAllStates() - Test all states');
console.log('- testScenarios.testNames() - Test name formats');
console.log('- testScenarios.testAnswers() - Test answer formats');
