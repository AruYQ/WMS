// wwwroot/js/auth.js

/**
 * WMS Authentication JavaScript Module
 * Handles login form interactions, validation, and UI enhancements
 */

(function () {
    'use strict';

    // Initialize when DOM is loaded
    document.addEventListener('DOMContentLoaded', function () {
        initializeAuthForms();
        initializePasswordToggles();
        initializeFormValidation();
        initializeAccessibility();
        initializeAnimations();
    });

    /**
     * Initialize authentication forms
     */
    function initializeAuthForms() {
        // Login form
        const loginForm = document.getElementById('loginForm');
        if (loginForm) {
            setupLoginForm(loginForm);
        }

        // Forgot password form
        const forgotPasswordForm = document.getElementById('forgotPasswordForm');
        if (forgotPasswordForm) {
            setupForgotPasswordForm(forgotPasswordForm);
        }

        // Reset password form
        const resetPasswordForm = document.getElementById('resetPasswordForm');
        if (resetPasswordForm) {
            setupResetPasswordForm(resetPasswordForm);
        }

        // Change password form
        const changePasswordForm = document.getElementById('changePasswordForm');
        if (changePasswordForm) {
            setupChangePasswordForm(changePasswordForm);
        }
    }

    /**
     * Setup login form functionality
     */
    function setupLoginForm(form) {
        const submitButton = form.querySelector('#loginButton');
        const usernameInput = form.querySelector('#UsernameOrEmail');
        const passwordInput = form.querySelector('#Password');
        const rememberMeCheckbox = form.querySelector('#RememberMe');

        // Form submission handling
        form.addEventListener('submit', function (e) {
            if (!validateLoginForm(form)) {
                e.preventDefault();
                return false;
            }

            // Show loading state
            if (submitButton && !submitButton.disabled) {
                setButtonLoading(submitButton, 'Memproses...', 'fas fa-spinner fa-spin');

                // Re-enable after timeout as fallback
                setTimeout(() => {
                    resetButton(submitButton, 'Masuk', 'fas fa-sign-in-alt');
                }, 15000);
            }
        });

        // Auto-focus username field
        if (usernameInput) {
            usernameInput.focus();
        }

        // Remember username if checkbox is checked
        if (rememberMeCheckbox && usernameInput) {
            // Load remembered username
            const rememberedUsername = localStorage.getItem('wms_remembered_username');
            if (rememberedUsername && rememberMeCheckbox.checked) {
                usernameInput.value = rememberedUsername;
            }

            // Save username when checkbox changes
            rememberMeCheckbox.addEventListener('change', function () {
                if (this.checked && usernameInput.value.trim()) {
                    localStorage.setItem('wms_remembered_username', usernameInput.value.trim());
                } else {
                    localStorage.removeItem('wms_remembered_username');
                }
            });
        }

        // Clear error states on input
        [usernameInput, passwordInput].forEach(input => {
            if (input) {
                input.addEventListener('input', function () {
                    clearFieldError(this);
                });
            }
        });
    }

    /**
     * Setup forgot password form
     */
    function setupForgotPasswordForm(form) {
        const submitButton = form.querySelector('#forgotPasswordButton');
        const emailInput = form.querySelector('#Email');

        form.addEventListener('submit', function (e) {
            if (!validateEmail(emailInput.value)) {
                e.preventDefault();
                showFieldError(emailInput, 'Format email tidak valid');
                return false;
            }

            if (submitButton && !submitButton.disabled) {
                setButtonLoading(submitButton, 'Mengirim...', 'fas fa-spinner fa-spin');
            }
        });

        if (emailInput) {
            emailInput.addEventListener('input', function () {
                clearFieldError(this);
            });
        }
    }

    /**
     * Setup reset password form
     */
    function setupResetPasswordForm(form) {
        const submitButton = form.querySelector('#resetPasswordButton');
        const newPasswordInput = form.querySelector('#NewPassword');
        const confirmPasswordInput = form.querySelector('#ConfirmPassword');

        form.addEventListener('submit', function (e) {
            if (!validatePasswordMatch(newPasswordInput, confirmPasswordInput)) {
                e.preventDefault();
                return false;
            }

            if (submitButton && !submitButton.disabled) {
                setButtonLoading(submitButton, 'Memproses...', 'fas fa-spinner fa-spin');
            }
        });

        // Real-time password confirmation validation
        if (confirmPasswordInput) {
            confirmPasswordInput.addEventListener('blur', function () {
                validatePasswordMatch(newPasswordInput, confirmPasswordInput);
            });
        }
    }

    /**
     * Setup change password form
     */
    function setupChangePasswordForm(form) {
        const submitButton = form.querySelector('#changePasswordButton');
        const currentPasswordInput = form.querySelector('#CurrentPassword');
        const newPasswordInput = form.querySelector('#NewPassword');
        const confirmPasswordInput = form.querySelector('#ConfirmPassword');

        form.addEventListener('submit', function (e) {
            let isValid = true;

            // Validate current password
            if (!currentPasswordInput.value.trim()) {
                showFieldError(currentPasswordInput, 'Password saat ini wajib diisi');
                isValid = false;
            }

            // Validate password match
            if (!validatePasswordMatch(newPasswordInput, confirmPasswordInput)) {
                isValid = false;
            }

            if (!isValid) {
                e.preventDefault();
                return false;
            }

            if (submitButton && !submitButton.disabled) {
                setButtonLoading(submitButton, 'Memproses...', 'fas fa-spinner fa-spin');
            }
        });

        // Clear errors on input
        [currentPasswordInput, newPasswordInput, confirmPasswordInput].forEach(input => {
            if (input) {
                input.addEventListener('input', function () {
                    clearFieldError(this);
                });
            }
        });
    }

    /**
     * Initialize password toggle functionality
     */
    function initializePasswordToggles() {
        // Generic password toggle setup
        const toggleButtons = document.querySelectorAll('[id^="toggle"][id$="Password"]');

        toggleButtons.forEach(button => {
            button.addEventListener('click', function () {
                const buttonId = this.id;
                const inputId = buttonId.replace('toggle', '').replace('Password', 'Password');
                const iconId = buttonId + 'Icon';

                const input = document.getElementById(inputId);
                const icon = document.getElementById(iconId);

                if (input && icon) {
                    if (input.type === 'password') {
                        input.type = 'text';
                        icon.className = 'fas fa-eye-slash';
                        this.setAttribute('aria-label', 'Sembunyikan password');
                    } else {
                        input.type = 'password';
                        icon.className = 'fas fa-eye';
                        this.setAttribute('aria-label', 'Tampilkan password');
                    }
                }
            });
        });
    }

    /**
     * Initialize form validation
     */
    function initializeFormValidation() {
        // Add Bootstrap validation classes
        const forms = document.querySelectorAll('.auth-form, form');

        forms.forEach(form => {
            const inputs = form.querySelectorAll('input[required]');

            inputs.forEach(input => {
                input.addEventListener('blur', function () {
                    validateField(this);
                });

                input.addEventListener('input', function () {
                    if (this.classList.contains('is-invalid')) {
                        clearFieldError(this);
                    }
                });
            });
        });
    }

    /**
     * Initialize accessibility features
     */
    function initializeAccessibility() {
        // Add aria labels to password toggle buttons
        const toggleButtons = document.querySelectorAll('[id^="toggle"][id$="Password"]');
        toggleButtons.forEach(button => {
            button.setAttribute('aria-label', 'Tampilkan password');
            button.setAttribute('type', 'button');
        });

        // Add role attributes to alerts
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            if (!alert.getAttribute('role')) {
                alert.setAttribute('role', 'alert');
            }
        });

        // Keyboard navigation for custom elements
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && e.target.classList.contains('password-toggle')) {
                e.target.click();
            }
        });
    }

    /**
     * Initialize animations and visual enhancements
     */
    function initializeAnimations() {
        // Animate form appearance
        const authCard = document.querySelector('.auth-card');
        if (authCard) {
            authCard.style.opacity = '0';
            authCard.style.transform = 'translateY(30px)';

            setTimeout(() => {
                authCard.style.transition = 'all 0.6s ease-out';
                authCard.style.opacity = '1';
                authCard.style.transform = 'translateY(0)';
            }, 100);
        }

        // Add focus animations to form controls
        const formControls = document.querySelectorAll('.form-control');
        formControls.forEach(control => {
            control.addEventListener('focus', function () {
                this.parentElement.classList.add('focused');
            });

            control.addEventListener('blur', function () {
                this.parentElement.classList.remove('focused');
            });
        });
    }

    /**
     * Validation helper functions
     */

    function validateLoginForm(form) {
        let isValid = true;
        const usernameInput = form.querySelector('#UsernameOrEmail');
        const passwordInput = form.querySelector('#Password');

        if (!usernameInput.value.trim()) {
            showFieldError(usernameInput, 'Username atau email wajib diisi');
            isValid = false;
        }

        if (!passwordInput.value.trim()) {
            showFieldError(passwordInput, 'Password wajib diisi');
            isValid = false;
        }

        return isValid;
    }

    function validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email.trim());
    }

    function validatePasswordMatch(newPasswordInput, confirmPasswordInput) {
        if (!newPasswordInput || !confirmPasswordInput) return true;

        const newPassword = newPasswordInput.value;
        const confirmPassword = confirmPasswordInput.value;

        if (newPassword && confirmPassword && newPassword !== confirmPassword) {
            showFieldError(confirmPasswordInput, 'Konfirmasi password tidak cocok');
            return false;
        }

        if (confirmPassword && newPassword === confirmPassword) {
            clearFieldError(confirmPasswordInput);
        }

        return true;
    }

    function validateField(input) {
        if (input.hasAttribute('required') && !input.value.trim()) {
            showFieldError(input, 'Field ini wajib diisi');
            return false;
        }

        if (input.type === 'email' && input.value && !validateEmail(input.value)) {
            showFieldError(input, 'Format email tidak valid');
            return false;
        }

        clearFieldError(input);
        return true;
    }

    /**
     * UI helper functions
     */

    function showFieldError(input, message) {
        input.classList.add('is-invalid');

        // Remove existing error message
        const existingError = input.parentElement.querySelector('.invalid-feedback');
        if (existingError) {
            existingError.remove();
        }

        // Add new error message
        const errorDiv = document.createElement('div');
        errorDiv.className = 'invalid-feedback';
        errorDiv.textContent = message;
        input.parentElement.appendChild(errorDiv);

        // Focus the field
        input.focus();
    }

    function clearFieldError(input) {
        input.classList.remove('is-invalid');
        const errorMessage = input.parentElement.querySelector('.invalid-feedback');
        if (errorMessage) {
            errorMessage.remove();
        }
    }

    function setButtonLoading(button, text, iconClass) {
        button.disabled = true;
        button.dataset.originalText = button.innerHTML;
        button.innerHTML = `<i class="${iconClass} me-2"></i>${text}`;
    }

    function resetButton(button, text, iconClass) {
        button.disabled = false;
        button.innerHTML = `<i class="${iconClass}"></i> ${text}`;
    }

    /**
     * Public API
     */
    window.WMSAuth = {
        showFieldError: showFieldError,
        clearFieldError: clearFieldError,
        validateEmail: validateEmail,
        setButtonLoading: setButtonLoading,
        resetButton: resetButton
    };

})();