const { createApp } = Vue;

const App2 = {
   
    computed: {
        totalCalories() {
            return this.foodEntries.reduce((sum, entry) => sum + parseInt(entry.calories || 0), 0);
        },
        totalProtein() {
            return this.foodEntries.reduce((sum, entry) => sum + parseInt(entry.protein || 0), 0);
        },
        totalCarbs() {
            return this.foodEntries.reduce((sum, entry) => sum + parseInt(entry.carbs || 0), 0);
        },
        totalFat() {
            return this.foodEntries.reduce((sum, entry) => sum + parseInt(entry.fat || 0), 0);
        },
        todayEntries() {
            const today = new Date().toISOString().split('T')[0];
            return this.foodEntries.filter(entry => entry.date === today);
        }
    },
   
    mounted() {
        // Загрузка тестовых данных
        this.foodEntries = [
            {
                id: 1,
                name: 'Овсяная каша',
                calories: 150,
                protein: 5,
                carbs: 27,
                fat: 3,
                date: new Date().toISOString().split('T')[0],
                time: '08:00'
            },
            {
                id: 2,
                name: 'Куриная грудка',
                calories: 165,
                protein: 31,
                carbs: 0,
                fat: 3.6,
                date: new Date().toISOString().split('T')[0],
                time: '13:00'
            }
        ];
    },
    
}

createApp(App2).mount('#app2');