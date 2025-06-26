<script setup lang="ts">
import { ref } from 'vue'

interface Emits {
  (e: 'select-query', query: string): void
}

const emit = defineEmits<Emits>()

const sampleQueries = [
  {
    id: 1,
    title: "AI Enthusiasts",
    query: "List all participants interested in AI events",
    icon: "🤖",
    category: "Participants",
    description: "Find people interested in artificial intelligence"
  },
  {
    id: 2,
    title: "Marketing Team",
    query: "Show me participants from the Marketing group",
    icon: "📈",
    category: "Groups",
    description: "View all marketing department members"
  },
  {
    id: 3,
    title: "Popular Events",
    query: "Find all events with more than 5 interested participants",
    icon: "🎉",
    category: "Events",
    description: "Discover high-engagement events"
  },
  {
    id: 4,
    title: "Top Interests",
    query: "What are the most popular interests?",
    icon: "⭐",
    category: "Analytics",
    description: "Analyze trending topics and interests"
  },
  {
    id: 5,
    title: "Event Participation",
    query: "Show participant count by event",
    icon: "👥",
    category: "Analytics",
    description: "Compare event attendance numbers"
  },
  {
    id: 6,
    title: "Group Distribution",
    query: "How many participants are in each group?",
    icon: "📊",
    category: "Groups",
    description: "Analyze team size distribution"
  }
]

const selectedCategory = ref('All')
const categories = ['All', 'Participants', 'Groups', 'Events', 'Analytics']

const filteredQueries = computed(() => {
  if (selectedCategory.value === 'All') {
    return sampleQueries
  }
  return sampleQueries.filter(q => q.category === selectedCategory.value)
})

const selectQuery = (query: string) => {
  emit('select-query', query)
}
</script>

<template>
  <div class="sticky top-24">
    <div class="card">
      <div class="mb-6">
        <h3 class="text-xl font-semibold text-slate-800 mb-2 flex items-center">
          <svg class="w-5 h-5 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
          </svg>
          Sample Queries
        </h3>
        <p class="text-sm text-slate-600">Click any query to try it instantly</p>
      </div>

      <!-- Category Filter -->
      <div class="mb-4">
        <div class="flex flex-wrap gap-1">
          <button
            v-for="category in categories"
            :key="category"
            @click="selectedCategory = category"
            class="px-3 py-1 text-xs font-medium rounded-full transition-all duration-200"
            :class="selectedCategory === category 
              ? 'bg-blue-100 text-blue-700 border border-blue-200' 
              : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
          >
            {{ category }}
          </button>
        </div>
      </div>

      <!-- Query List -->
      <div class="space-y-3 max-h-96 overflow-y-auto">
        <div
          v-for="sample in filteredQueries"
          :key="sample.id"
          @click="selectQuery(sample.query)"
          class="group p-4 bg-gradient-to-r from-slate-50 to-blue-50 hover:from-blue-50 hover:to-indigo-50 rounded-lg border border-slate-200 hover:border-blue-300 cursor-pointer transition-all duration-200 hover:shadow-md"
        >
          <div class="flex items-start space-x-3">
            <div class="flex-shrink-0">
              <span class="text-2xl">{{ sample.icon }}</span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center justify-between mb-1">
                <h4 class="text-sm font-semibold text-slate-800 group-hover:text-blue-700 transition-colors">
                  {{ sample.title }}
                </h4>
                <span class="text-xs px-2 py-1 bg-white/60 text-slate-600 rounded-full">
                  {{ sample.category }}
                </span>
              </div>
              <p class="text-xs text-slate-600 mb-2 line-clamp-2">
                {{ sample.description }}
              </p>
              <div class="text-xs text-blue-600 font-mono bg-white/50 p-2 rounded border group-hover:bg-blue-50 transition-colors">
                "{{ sample.query }}"
              </div>
            </div>
          </div>
          <div class="mt-3 flex items-center justify-end">
            <svg class="w-4 h-4 text-slate-400 group-hover:text-blue-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"/>
            </svg>
          </div>
        </div>
      </div>

      <!-- Tips Section -->
      <div class="mt-6 p-4 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200 rounded-lg">
        <div class="flex items-start space-x-2">
          <span class="text-lg">💡</span>
          <div class="text-sm">
            <p class="font-medium text-amber-800 mb-1">Pro Tips:</p>
            <ul class="text-amber-700 space-y-1 text-xs">
              <li>• Be specific in your questions</li>
              <li>• Use table/column names when known</li>
              <li>• Ask for counts, averages, or comparisons</li>
              <li>• Try different chart types for visualization</li>
            </ul>
          </div>
        </div>
      </div>

      <!-- Quick Stats -->
      <div class="mt-4 grid grid-cols-2 gap-3">
        <div class="text-center p-3 bg-gradient-to-br from-green-50 to-emerald-50 border border-green-200 rounded-lg">
          <div class="text-lg font-bold text-green-600">{{ sampleQueries.length }}</div>
          <div class="text-xs text-green-700">Sample Queries</div>
        </div>
        <div class="text-center p-3 bg-gradient-to-br from-purple-50 to-violet-50 border border-purple-200 rounded-lg">
          <div class="text-lg font-bold text-purple-600">{{ categories.length - 1 }}</div>
          <div class="text-xs text-purple-700">Categories</div>
        </div>
      </div>
    </div>
  </div>
</template>