const { createApp } = Vue;

const App = {
    data() {
        return {
            currentView: 'diary',
            foodForm: {
                name: '',
                calories: '0',
                protein: '',
                carbs: '',
                fat: '',
                date: new Date().toISOString().split('T')[0],
                time: new Date().toTimeString().slice(0, 5)
            },
            editingEntry: null,
            foodEntries: [],
            showAlert: false,
            alertMessage: '',
            alertType: 'success',
            selectedDate: new Date().toISOString().split('T')[0]
        }
    },
    computed: {
        totalCalories() {
            return this.filteredEntries.reduce((sum, entry) => sum + parseInt(entry.calories || 0), 0);
        },
        totalProtein() {
            return this.filteredEntries.reduce((sum, entry) => sum + parseInt(entry.protein || 0), 0);
        },
        totalCarbs() {
            return this.filteredEntries.reduce((sum, entry) => sum + parseInt(entry.carbs || 0), 0);
        },
        totalFat() {
            return this.filteredEntries.reduce((sum, entry) => sum + parseInt(entry.fat || 0), 0);
        },
        filteredEntries() {
            return this.foodEntries.filter(entry => entry.date === this.selectedDate)
                .sort((a, b) => a.time.localeCompare(b.time));
        },
        allDates() {
            const dates = [...new Set(this.foodEntries.map(entry => entry.date))];
            return dates.sort().reverse();
        }
    },
    methods: {
        addFoodEntry() {
            if (!this.foodForm.name || !this.foodForm.calories) {
                this.showAlertMessage('Заполните название и калории', 'error');
                return;
            }
            
            const entry = {
                id: Date.now(),
                ...this.foodForm
            };
            
            this.foodEntries.unshift(entry);
            this.showAlertMessage('Прием пищи добавлен в дневник!', 'success');
            this.resetFoodForm();
            this.saveToLocalStorage();
        },
        
        editFoodEntry(entry) {
            this.editingEntry = { ...entry };
            this.foodForm = { ...entry };
        },

        updateFoodEntry() {
            if (!this.foodForm.name || !this.foodForm.calories) {
                this.showAlertMessage('Заполните название и калории', 'error');
                return;
            }
            
            const index = this.foodEntries.findIndex(entry => entry.id === this.editingEntry.id);
            if (index !== -1) {
                this.foodEntries[index] = { ...this.foodForm, id: this.editingEntry.id };
                this.showAlertMessage('Запись обновлена!', 'success');
                this.cancelEdit();
                this.saveToLocalStorage();
            }
        },
        
        cancelEdit() {
            this.editingEntry = null;
            this.resetFoodForm();
        },
        
        deleteFoodEntry(id) {
            if (confirm('Вы уверены, что хотите удалить эту запись?')) {
                this.foodEntries = this.foodEntries.filter(entry => entry.id !== id);
                this.showAlertMessage('Запись удалена из дневника', 'success');
                this.saveToLocalStorage();
            }
        },
        
        resetFoodForm() {
            this.foodForm = {
                name: '',
                calories: '',
                protein: '',
                carbs: '',
                fat: '',
                date: new Date().toISOString().split('T')[0],
                time: new Date().toTimeString().slice(0, 5)
            };
        },
        
        showAlertMessage(message, type) {
            this.alertMessage = message;
            this.alertType = type;
            this.showAlert = true;
            setTimeout(() => {
                this.showAlert = false;
            }, 4000);
        },
        
        goToRegistration() {
            this.saveToLocalStorage();
            window.location.href = 'entry.html';
        },
        
        saveToLocalStorage() {
            localStorage.setItem('healsEatFoodEntries', JSON.stringify(this.foodEntries));
        },
        
        loadFromLocalStorage() {
            const saved = localStorage.getItem('healsEatFoodEntries');
            if (saved) {
                this.foodEntries = JSON.parse(saved);
            }
        },
        
        formatTime(time) {
            return time;
        },
        
        getDateDisplay(date) {
            const today = new Date().toISOString().split('T')[0];
            const yesterday = new Date(Date.now() - 86400000).toISOString().split('T')[0];
            
            if (date === today) return 'Сегодня';
            if (date === yesterday) return 'Вчера';
            
            return new Date(date).toLocaleDateString('ru-RU');
        }
    },
    mounted() {
        this.loadFromLocalStorage();
        
        if (this.foodEntries.length === 0) {
            this.foodEntries = [
                {
                    id: 1,
                    name: 'О',
                    calories: 250,
                    protein: 8,
                    carbs: 45,
                    fat: 4,
                    date: new Date().toISOString().split('T')[0],
                    time: '08:30'
                },
                {
                    id: 2,
                    name: 'С',
                    calories: 320,
                    protein: 25,
                    carbs: 12,
                    fat: 8,
                    date: new Date().toISOString().split('T')[0],
                    time: '13:15'
                }
            ];
            this.saveToLocalStorage();
        }
    },
    createApp(App){mount('#app')}
}
//createApp(App).mount('#app');